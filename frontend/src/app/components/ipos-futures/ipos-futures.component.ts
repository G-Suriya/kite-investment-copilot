import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import {
  MultiSourceMarketPulse,
  IpoGmpItem,
  FuturesTraction,
  CompanyFinancials,
  GrowthOpportunitiesResponse,
  StockGrowthOpportunity,
  IpoGrowthOpportunity
} from '../../models/copilot.models';

@Component({
  selector: 'app-ipos-futures',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ipos-futures.component.html',
  styleUrls: ['./ipos-futures.component.css']
})
export class IposFuturesComponent implements OnInit, OnDestroy {
  activeTab: 'ipos' | 'growth-picks' | 'futures' | 'market' | 'revenue-drivers' = 'ipos';

  marketPulse: MultiSourceMarketPulse | null = null;
  ipos: IpoGmpItem[] = [];
  futures: FuturesTraction[] = [];
  financials: CompanyFinancials | null = null;
  growthOpportunities: GrowthOpportunitiesResponse | null = null;

  // === Groww-Style State & Filters ===
  stageFilter: 'UPCOMING' | 'OPEN' | 'CLOSED' | 'SUBSCRIPTION' | 'ALLOTMENT' = 'UPCOMING';
  ipoTypeFilter: 'ALL' | 'Mainboard' | 'SME' = 'ALL';
  searchQuery: string = '';

  // === Customization Toggles ===
  showGmp: boolean = true;
  showPriceBand: boolean = true;
  showMinInvestment: boolean = true;
  showIssueSize: boolean = false;
  showLotSize: boolean = false;
  showAllotmentRegistrar: boolean = false;
  showAiAnalysis: boolean = true;
  viewMode: 'table' | 'cards' = 'table';
  showCustomizerMenu: boolean = false;

  selectedStockSymbol: string = 'HDFCBANK';
  availableStocks: string[] = ['HDFCBANK', 'TCS', 'INFY', 'ITC', 'HINDUNILVR', 'RELIANCE'];

  isLoading = true;
  isLoadingFinancials = false;
  gmpFilter: 'ALL' | 'HIGH' | 'MODERATE' = 'ALL';
  private autoPollTimer: any;

  constructor(
    private api: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadAllData();
    // Continuous background sync every 20s
    this.autoPollTimer = setInterval(() => {
      this.refreshSilently();
    }, 20000);
  }

  ngOnDestroy(): void {
    if (this.autoPollTimer) clearInterval(this.autoPollTimer);
  }

  loadAllData(): void {
    this.isLoading = true;

    this.api.getGrowthSuggestions().subscribe({
      next: (g) => this.growthOpportunities = g,
      error: () => {}
    });

    this.api.getMarketOverview().subscribe({
      next: (pulse) => this.marketPulse = pulse,
      error: () => {}
    });

    this.api.getIpos().subscribe({
      next: (ipos) => this.ipos = ipos,
      error: () => {}
    });

    this.api.getFutures().subscribe({
      next: (futures) => this.futures = futures,
      error: () => {}
    });

    this.loadStockFinancials(this.selectedStockSymbol);
    this.isLoading = false;
  }

  refreshSilently(): void {
    this.api.getGrowthSuggestions().subscribe({
      next: (g) => this.growthOpportunities = g
    });
    this.api.getIpos().subscribe({
      next: (ipos) => this.ipos = ipos
    });
    this.api.getFutures().subscribe({
      next: (futures) => this.futures = futures
    });
    this.api.getMarketOverview().subscribe({
      next: (pulse) => this.marketPulse = pulse
    });
  }

  loadStockFinancials(symbol: string): void {
    this.selectedStockSymbol = symbol;
    this.isLoadingFinancials = true;
    this.api.getCompanyFinancials(symbol).subscribe({
      next: (fin) => {
        this.financials = fin;
        this.isLoadingFinancials = false;
      },
      error: () => {
        this.isLoadingFinancials = false;
      }
    });
  }

  get filteredIpos(): IpoGmpItem[] {
    return this.growwFilteredIpos;
  }

  get growwFilteredIpos(): IpoGmpItem[] {
    let list = this.ipos;

    // Filter by Stage
    if (this.stageFilter === 'UPCOMING') {
      list = list.filter(i => (i.stage === 'UPCOMING') || i.status?.toUpperCase().includes('UPCOMING'));
    } else if (this.stageFilter === 'OPEN') {
      list = list.filter(i => (i.stage === 'OPEN') || i.status?.toUpperCase().includes('OPEN'));
    } else if (this.stageFilter === 'CLOSED') {
      list = list.filter(i => (i.stage === 'CLOSED') || i.status?.toUpperCase().includes('CLOSED') || i.status?.toUpperCase().includes('LISTED'));
    } else if (this.stageFilter === 'SUBSCRIPTION') {
      list = list.filter(i => i.subscriptionTimes > 0 || i.stage === 'OPEN');
    } else if (this.stageFilter === 'ALLOTMENT') {
      list = list.filter(i => !!i.allotmentDate || i.stage === 'CLOSED' || i.stage === 'OPEN');
    }

    // Filter by IPO Type
    if (this.ipoTypeFilter === 'Mainboard') {
      list = list.filter(i => i.ipoType?.toLowerCase() === 'mainboard');
    } else if (this.ipoTypeFilter === 'SME') {
      list = list.filter(i => i.ipoType?.toLowerCase() === 'sme');
    }

    // Search query filter
    if (this.searchQuery && this.searchQuery.trim().length > 0) {
      const q = this.searchQuery.toLowerCase().trim();
      list = list.filter(i =>
        i.companyName.toLowerCase().includes(q) ||
        i.symbol.toLowerCase().includes(q) ||
        (i.sector && i.sector.toLowerCase().includes(q))
      );
    }

    return list;
  }

  getStageCount(stage: 'UPCOMING' | 'OPEN' | 'CLOSED' | 'SUBSCRIPTION' | 'ALLOTMENT'): number {
    if (stage === 'UPCOMING') {
      return this.ipos.filter(i => i.stage === 'UPCOMING' || i.status?.toUpperCase().includes('UPCOMING')).length;
    }
    if (stage === 'OPEN') {
      return this.ipos.filter(i => i.stage === 'OPEN' || i.status?.toUpperCase().includes('OPEN')).length;
    }
    if (stage === 'CLOSED') {
      return this.ipos.filter(i => i.stage === 'CLOSED' || i.status?.toUpperCase().includes('CLOSED') || i.status?.toUpperCase().includes('LISTED')).length;
    }
    if (stage === 'SUBSCRIPTION') {
      return this.ipos.filter(i => i.subscriptionTimes > 0 || i.stage === 'OPEN').length;
    }
    if (stage === 'ALLOTMENT') {
      return this.ipos.filter(i => !!i.allotmentDate || i.stage === 'CLOSED' || i.stage === 'OPEN').length;
    }
    return 0;
  }

  getStageTitle(): string {
    switch (this.stageFilter) {
      case 'UPCOMING': return 'Upcoming - IPOs';
      case 'OPEN': return 'Open - IPOs';
      case 'CLOSED': return 'Closed - IPOs';
      case 'SUBSCRIPTION': return 'Live Subscription - IPOs';
      case 'ALLOTMENT': return 'Allotment Status - IPOs';
      default: return 'IPOs';
    }
  }

  toggleCustomizer(): void {
    this.showCustomizerMenu = !this.showCustomizerMenu;
  }

  resetCustomization(): void {
    this.showGmp = true;
    this.showPriceBand = true;
    this.showMinInvestment = true;
    this.showIssueSize = false;
    this.showLotSize = false;
    this.showAllotmentRegistrar = false;
    this.showAiAnalysis = true;
    this.viewMode = 'table';
  }

  getLogoInitials(ipo: IpoGmpItem): string {
    if (ipo.logoInitials) return ipo.logoInitials;
    const parts = ipo.companyName.split(' ');
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return ipo.companyName.substring(0, 2).toUpperCase();
  }

  openDocUrl(ipo: IpoGmpItem, event?: MouseEvent): void {
    if (event) event.stopPropagation();
    if (ipo.docUrl) {
      window.open(ipo.docUrl, '_blank');
    } else {
      // Direct to SEBI search
      window.open(`https://www.sebi.gov.in/sebiweb/home/HomeAction.do?doListing=yes&sid=3&ssid=15&smid=0`, '_blank');
    }
  }

  openRegistrarUrl(ipo: IpoGmpItem, event?: MouseEvent): void {
    if (event) event.stopPropagation();
    if (ipo.registrarUrl) {
      window.open(ipo.registrarUrl, '_blank');
    } else {
      window.open('https://linkintime.co.in/initial_offer/public-issues.html', '_blank');
    }
  }

  getRevenueBarWidth(revenue: number, maxRevenue: number = 100000): number {
    if (!maxRevenue || maxRevenue <= 0) return 50;
    const pct = (revenue / maxRevenue) * 100;
    return Math.min(Math.max(pct, 10), 100);
  }

  askCopilotAboutIpo(ipo: IpoGmpItem): void {
    const prompt = `Analyze the ${ipo.companyName} (${ipo.symbol}) IPO. Issue price is ${ipo.priceBand}, grey market premium (GMP) is ₹${ipo.gmp} (+${ipo.gmpPercent?.toFixed(1)}%). Explain listing expectations, risks, valuation, and whether to apply in simple terms.`;
    this.router.navigate(['/chat'], { queryParams: { prompt } });
  }

  askCopilotAboutStock(symbol: string): void {
    const prompt = `Why is ${symbol} falling or going up recently? Explain company quarterly revenue, profit drivers, headwinds, and technical levels.`;
    this.router.navigate(['/chat'], { queryParams: { prompt } });
  }

  askCopilotAboutFuture(f: FuturesTraction): void {
    const prompt = `Analyze ${f.symbol} futures contract (${f.expiryDate}). Current futures price is ₹${f.futuresPrice}, Open Interest change is ${f.openInterestChangePercent}%, PCR is ${f.putCallRatio} (${f.pcrSentiment}), setup is "${f.buildupType}". Explain what this means for institutional direction.`;
    this.router.navigate(['/chat'], { queryParams: { prompt } });
  }

  askCopilotAboutOpportunity(s: StockGrowthOpportunity): void {
    const prompt = `Analyze ${s.companyName} (${s.symbol}). CMP is ₹${s.currentPrice}, Target is ₹${s.targetPrice} (+${s.upsidePotentialPercent}% upside), YoY growth is ${s.yoYRevenueGrowthPercent}%, ROCE is ${s.rocePercent}%. Growth theme is "${s.growthTheme}". Detail the growth thesis, catalysts, risks, and entry levels.`;
    this.router.navigate(['/chat'], { queryParams: { prompt } });
  }

  askCopilotAboutIpoOpp(ipo: IpoGrowthOpportunity): void {
    const prompt = `Provide full analysis for ${ipo.companyName} (${ipo.symbol}) IPO. Issue price is ${ipo.priceBand}, GMP is ₹${ipo.gmp} (+${ipo.gmpPercent}%), YoY growth is ${ipo.yoYRevenueGrowthPercent}%. Financial summary: ${ipo.financialHealthSummary}. Should I apply for listing gains or long-term compounding?`;
    this.router.navigate(['/chat'], { queryParams: { prompt } });
  }
}
