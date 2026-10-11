import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ApiService } from '../../services/api.service';
import { Holding, Position, Funds, Order, KiteProfile } from '../../models/copilot.models';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './portfolio.component.html',
  styleUrls: ['./portfolio.component.css']
})
export class PortfolioComponent implements OnInit, OnDestroy {
  activeTab: 'holdings' | 'positions' | 'funds' | 'orders' | 'chart' = 'holdings';
  selectedSymbol: string = 'HDFCBANK';
  
  holdings: Holding[] = [];
  positions: Position[] = [];
  funds: Funds = { availableMargin: 0, usedMargin: 0, cashBalance: 0, collateralMargin: 0, totalMargin: 0, currency: 'INR' };
  orders: Order[] = [];
  profile: KiteProfile = { userId: '', userName: '', userType: '', email: '', broker: '', isConnected: false };

  totalInvested = 0;
  totalCurrent = 0;
  totalPnL = 0;
  totalPnLPercent = 0;
  isLoading = true;
  isRefreshing = false;
  private pollTimer: any;

  constructor(
    private api: ApiService, 
    private router: Router,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.loadAll();
    // Continuous live quote sync every 15 seconds
    this.pollTimer = setInterval(() => {
      this.refreshSilently();
    }, 15000);
  }

  ngOnDestroy(): void {
    if (this.pollTimer) clearInterval(this.pollTimer);
  }

  refreshSilently(): void {
    this.api.getHoldings().subscribe({
      next: (h) => {
        if (h && h.length > 0) {
          this.holdings = h;
          this.totalInvested = this.holdings.reduce((s, i) => s + (i.investedValue || (i.averagePrice * i.quantity)), 0);
          this.totalCurrent = this.holdings.reduce((s, i) => s + (i.currentValue || (i.lastPrice * i.quantity)), 0);
          this.totalPnL = this.holdings.reduce((s, i) => s + (i.pnL || (i.lastPrice - i.averagePrice) * i.quantity), 0);
          this.totalPnLPercent = this.totalInvested > 0 ? (this.totalPnL / this.totalInvested) * 100 : 0;
        }
      }
    });
  }

  loadAll(): void {
    this.isRefreshing = true;
    this.api.getProfile().subscribe({
      next: p => this.profile = p,
      error: () => {}
    });

    this.api.getFunds().subscribe({
      next: f => this.funds = f,
      error: () => {}
    });

    this.api.getHoldings().subscribe({
      next: (h) => {
        this.holdings = h || [];
        if (this.holdings.length > 0) {
          if (!this.selectedSymbol || !this.holdings.some(x => x.symbol === this.selectedSymbol)) {
            this.selectedSymbol = this.holdings[0].symbol;
          }
        }
        this.totalInvested = this.holdings.reduce((s, i) => s + (i.investedValue || (i.averagePrice * i.quantity)), 0);
        this.totalCurrent = this.holdings.reduce((s, i) => s + (i.currentValue || (i.lastPrice * i.quantity)), 0);
        this.totalPnL = this.holdings.reduce((s, i) => s + (i.pnL || (i.lastPrice - i.averagePrice) * i.quantity), 0);
        this.totalPnLPercent = this.totalInvested > 0 ? (this.totalPnL / this.totalInvested) * 100 : 0;
        this.isLoading = false;
        this.isRefreshing = false;
      },
      error: () => {
        this.isLoading = false;
        this.isRefreshing = false;
      }
    });

    this.api.getPositions().subscribe({
      next: p => this.positions = p || [],
      error: () => {}
    });

    this.api.getOrders().subscribe({
      next: o => this.orders = o || [],
      error: () => {}
    });
  }

  setTab(tab: 'holdings' | 'positions' | 'funds' | 'orders' | 'chart'): void {
    this.activeTab = tab;
  }

  viewChart(symbol: string): void {
    this.selectedSymbol = symbol;
    this.activeTab = 'chart';
  }

  getChartUrl(symbol: string): SafeResourceUrl {
    const s = (symbol || 'HDFCBANK').trim().toUpperCase();
    const isBse = s === 'MON100' || s === 'GOLDBEES' || s === 'HINDUNILVR' || s === 'TCS' || s === 'ITC';
    const exchange = isBse ? 'BSE' : 'NSE';
    const url = `https://s.tradingview.com/widgetembed/?frameElementId=tradingview_widget&symbol=${exchange}%3A${s}&interval=D&hidesidetoolbar=0&symboledit=1&saveimage=1&toolbarbg=FFFFFF&studies=%5B%5D&theme=light&style=1&timezone=Asia%2FKolkata&locale=en`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  analyzeSymbol(symbol: string): void {
    this.router.navigate(['/chat'], { queryParams: { prompt: `Analyse ${symbol} from my holdings` } });
  }

  connectZerodha(): void {
    this.api.getKiteLoginUrl().subscribe(res => {
      if (res && res.loginUrl) {
        window.location.href = res.loginUrl;
      }
    });
  }
}
