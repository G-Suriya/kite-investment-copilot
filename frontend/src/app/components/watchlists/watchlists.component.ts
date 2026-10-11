import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ApiService } from '../../services/api.service';
import { Watchlist, WatchlistItem, MarketQuote } from '../../models/copilot.models';

export type WatchlistItemWithMeta = WatchlistItem & { watchlistNames?: string[] };

@Component({
  selector: 'app-watchlists',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './watchlists.component.html',
  styleUrls: ['./watchlists.component.css']
})
export class WatchlistsComponent implements OnInit {
  watchlists: Watchlist[] = [];
  selectedWatchlistId: number = 0; // 0 represents the 'All Watchlist Stocks' master view
  selectedTypeFilter: 'ALL' | 'STOCK' | 'ETF' | 'IPO' | 'INDEX' = 'ALL';
  searchQuery: string = '';
  quotesMap: Record<string, MarketQuote> = {};
  isLoading = true;
  isSyncing = false;
  syncMessage: string | null = null;
  selectedChartSymbol: string | null = null;
  previewChartSymbol: string = 'HDFCBANK';
  showSideChart: boolean = false;

  // Add Item Modal state
  showAddModal = false;
  newItemSymbol = '';
  newItemExchange = 'NSE';
  newItemAssetType = 'STOCK';
  newItemTargetPrice: number | null = null;
  newItemNotes = '';
  selectedTargetWatchlistId: number = 0;

  // Create Watchlist Modal state
  showCreateModal = false;
  newWatchlistName = '';
  newWatchlistType = 'Stocks';
  newWatchlistDesc = '';

  constructor(
    private api: ApiService, 
    private router: Router,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.loadWatchlists();
  }

  get isAllWatchlists(): boolean {
    return this.selectedWatchlistId === 0;
  }

  get allWatchlistItems(): WatchlistItemWithMeta[] {
    const map = new Map<string, WatchlistItemWithMeta>();
    for (const wl of this.watchlists) {
      for (const item of (wl.items || [])) {
        const key = item.symbol.toUpperCase();
        if (map.has(key)) {
          const existing = map.get(key)!;
          if (existing.watchlistNames && !existing.watchlistNames.includes(wl.name)) {
            existing.watchlistNames.push(wl.name);
          }
        } else {
          map.set(key, {
            ...item,
            watchlistNames: [wl.name]
          });
        }
      }
    }
    return Array.from(map.values());
  }

  get activeWatchlist(): (Watchlist & { isAll?: boolean }) | undefined {
    if (this.isAllWatchlists) {
      return {
        id: 0,
        name: '🌟 Live Kite Watchlist',
        type: 'Master View',
        description: 'Consolidated real-time watchlist of all live Zerodha Kite holdings, stocks, and ETFs.',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        items: this.allWatchlistItems,
        isAll: true
      };
    }
    return this.watchlists.find(w => w.id === this.selectedWatchlistId);
  }

  get filteredItems(): WatchlistItemWithMeta[] {
    if (!this.activeWatchlist) return [];
    let items: WatchlistItemWithMeta[] = this.activeWatchlist.items || [];
    if (this.selectedTypeFilter !== 'ALL') {
      items = items.filter(i => (i.assetType || 'STOCK').toUpperCase() === this.selectedTypeFilter);
    }
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      items = items.filter(i => 
        i.symbol.toLowerCase().includes(q) || 
        (i.companyName && i.companyName.toLowerCase().includes(q)) ||
        (i.notes && i.notes.toLowerCase().includes(q)) ||
        (i.watchlistNames && i.watchlistNames.some(w => w.toLowerCase().includes(q)))
      );
    }
    return items;
  }

  getTypeCount(type: string): number {
    if (!this.activeWatchlist || !this.activeWatchlist.items) return 0;
    if (type === 'ALL') return this.activeWatchlist.items.length;
    return this.activeWatchlist.items.filter(i => (i.assetType || 'STOCK').toUpperCase() === type).length;
  }

  syncKiteHoldings(): void {
    this.isSyncing = true;
    this.syncMessage = null;
    this.api.syncKiteHoldings().subscribe({
      next: (res) => {
        this.isSyncing = false;
        this.syncMessage = res.message || `Successfully synced ${res.totalSynced} Kite holdings into categorized lists!`;
        this.loadWatchlists();
        setTimeout(() => {
          this.syncMessage = null;
        }, 6000);
      },
      error: (err) => {
        this.isSyncing = false;
        alert('Failed to sync Kite holdings: ' + (err.error?.message || err.message || 'Unknown error'));
      }
    });
  }

  loadWatchlists(): void {
    this.isLoading = true;
    this.api.getWatchlists().subscribe({
      next: (lists) => {
        this.watchlists = lists;
        if (this.selectedWatchlistId !== 0 && !lists.some(w => w.id === this.selectedWatchlistId)) {
          this.selectedWatchlistId = 0;
        }

        const items = this.filteredItems;
        if (items.length > 0 && (!this.previewChartSymbol || !items.some(x => x.symbol === this.previewChartSymbol))) {
          this.previewChartSymbol = items[0].symbol;
        }

        // Fetch real-time quotes for all symbols across watchlists
        const symbols = new Set<string>();
        lists.forEach(l => (l.items || []).forEach(i => symbols.add(i.symbol)));
        if (symbols.size > 0) {
          this.api.getQuotes(Array.from(symbols)).subscribe(quotes => {
            quotes.forEach(q => this.quotesMap[q.symbol.toUpperCase()] = q);
          });
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  selectWatchlist(id: number): void {
    this.selectedWatchlistId = id;
    this.selectedTypeFilter = 'ALL';
    const items = this.filteredItems;
    if (items.length > 0) {
      this.previewChartSymbol = items[0].symbol;
    }
  }

  selectTypeFilter(filter: 'ALL' | 'STOCK' | 'ETF' | 'IPO' | 'INDEX'): void {
    this.selectedTypeFilter = filter;
    const items = this.filteredItems;
    if (items.length > 0 && !items.some(i => i.symbol === this.previewChartSymbol)) {
      this.previewChartSymbol = items[0].symbol;
    }
  }

  selectPreviewStock(symbol: string): void {
    this.previewChartSymbol = symbol;
  }

  viewChart(symbol: string, event?: Event): void {
    if (event) event.stopPropagation();
    this.previewChartSymbol = symbol;
    this.selectedChartSymbol = symbol;
  }

  closeChart(): void {
    this.selectedChartSymbol = null;
  }

  toggleSideChart(): void {
    this.showSideChart = !this.showSideChart;
  }

  getChartUrl(symbol: string): SafeResourceUrl {
    const s = symbol.trim().toUpperCase();
    const isBse = s === 'MON100' || s === 'GOLDBEES' || s === 'HINDUNILVR' || s === 'TCS' || s === 'ITC';
    const exchange = isBse ? 'BSE' : 'NSE';
    const url = `https://s.tradingview.com/widgetembed/?frameElementId=tradingview_widget&symbol=${exchange}%3A${s}&interval=D&hidesidetoolbar=0&symboledit=1&saveimage=1&toolbarbg=FFFFFF&studies=%5B%5D&theme=light&style=1&timezone=Asia%2FKolkata&locale=en`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  getQuote(symbol: string): MarketQuote | undefined {
    return this.quotesMap[symbol.toUpperCase()];
  }

  calculate52WeekPercentage(quote?: MarketQuote): number {
    if (!quote || quote.fiftyTwoWeekHigh === quote.fiftyTwoWeekLow) return 50;
    const pct = ((quote.lastPrice - quote.fiftyTwoWeekLow) / (quote.fiftyTwoWeekHigh - quote.fiftyTwoWeekLow)) * 100;
    return Math.min(Math.max(pct, 0), 100);
  }

  openAddItem(): void {
    this.newItemSymbol = '';
    this.newItemNotes = '';
    this.newItemTargetPrice = null;
    this.selectedTargetWatchlistId = this.selectedWatchlistId !== 0 
      ? this.selectedWatchlistId 
      : (this.watchlists.length > 0 ? this.watchlists[0].id : 0);
    this.showAddModal = true;
  }

  submitAddItem(): void {
    if (!this.newItemSymbol.trim()) return;

    let targetWlId = Number(this.selectedTargetWatchlistId);
    if (!targetWlId || targetWlId === 0) {
      targetWlId = this.selectedWatchlistId !== 0 
        ? this.selectedWatchlistId 
        : (this.watchlists.length > 0 ? this.watchlists[0].id : 1);
    }

    this.api.addWatchlistItem(
      targetWlId,
      this.newItemSymbol.trim().toUpperCase(),
      this.newItemExchange,
      this.newItemAssetType,
      this.newItemNotes,
      this.newItemTargetPrice || undefined
    ).subscribe({
      next: () => {
        this.showAddModal = false;
        this.loadWatchlists();
      },
      error: (err) => {
        alert('Failed to add asset: ' + (err.error?.message || err.message || 'Unknown error'));
      }
    });
  }

  removeItem(symbol: string, event?: Event): void {
    if (event) event.stopPropagation();
    if (!confirm(`Remove ${symbol} from watchlist?`)) return;

    if (this.selectedWatchlistId === 0) {
      // Find the first watchlist containing this item to remove
      const wlWithItem = this.watchlists.find(w => w.items.some(i => i.symbol.toUpperCase() === symbol.toUpperCase()));
      if (wlWithItem) {
        this.api.removeWatchlistItem(wlWithItem.id, symbol).subscribe(() => this.loadWatchlists());
      }
    } else {
      this.api.removeWatchlistItem(this.selectedWatchlistId, symbol).subscribe(() => {
        this.loadWatchlists();
      });
    }
  }

  submitCreateWatchlist(): void {
    if (!this.newWatchlistName.trim()) return;

    this.api.createWatchlist(
      this.newWatchlistName.trim(),
      this.newWatchlistType,
      this.newWatchlistDesc.trim() || undefined
    ).subscribe(created => {
      this.showCreateModal = false;
      this.newWatchlistName = '';
      this.newWatchlistDesc = '';
      this.loadWatchlists();
      this.selectedWatchlistId = created.id;
    });
  }

  deleteActiveWatchlist(): void {
    if (!this.activeWatchlist || this.selectedWatchlistId === 0) return;
    if (!confirm(`Are you sure you want to delete '${this.activeWatchlist.name}' watchlist?`)) return;

    this.api.deleteWatchlist(this.activeWatchlist.id).subscribe(() => {
      this.selectedWatchlistId = 0;
      this.loadWatchlists();
    });
  }

  analyzeSymbol(symbol: string, event?: Event): void {
    if (event) event.stopPropagation();
    this.router.navigate(['/chat'], { queryParams: { prompt: `Analyse ${symbol} from my watchlist` } });
  }

  setQuickSymbol(sym: string, type: string): void {
    this.newItemSymbol = sym;
    this.newItemAssetType = type;
  }
}
