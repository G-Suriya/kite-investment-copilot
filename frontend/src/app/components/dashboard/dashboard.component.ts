import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Holding, Funds, Watchlist, KiteSessionInfo } from '../../models/copilot.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  holdings: Holding[] = [];
  funds: Funds = { availableMargin: 0, usedMargin: 0, cashBalance: 0, collateralMargin: 0, totalMargin: 0, currency: 'INR' };
  watchlists: Watchlist[] = [];
  kiteSession: KiteSessionInfo = { isAuthenticated: true, isDemoMode: false, apiKeyMasked: 'kite_live_****' };
  
  totalInvested = 0;
  totalCurrent = 0;
  totalPnL = 0;
  totalPnLPercent = 0;
  dayPnL = 0;
  dayPnLPercent = 0;
  isLoading = true;
  private autoRefreshTimer: any;

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.loadData();
    // Continuous live refresh every 20 seconds
    this.autoRefreshTimer = setInterval(() => {
      this.refreshSilently();
    }, 20000);
  }

  ngOnDestroy(): void {
    if (this.autoRefreshTimer) clearInterval(this.autoRefreshTimer);
  }

  loadData(): void {
    this.isLoading = true;

    this.api.getKiteStatus().subscribe(session => this.kiteSession = session);

    this.api.getHoldings().subscribe({
      next: (data) => {
        this.holdings = data;
        this.calculateMetrics();
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });

    this.api.getFunds().subscribe({
      next: (data) => this.funds = data
    });

    this.api.getWatchlists().subscribe({
      next: (data) => this.watchlists = data
    });
  }

  refreshSilently(): void {
    this.api.getHoldings().subscribe({
      next: (data) => {
        this.holdings = data;
        this.calculateMetrics();
      }
    });
    this.api.getFunds().subscribe({
      next: (data) => this.funds = data
    });
  }

  calculateMetrics(): void {
    this.totalInvested = this.holdings.reduce((sum, h) => sum + h.investedValue, 0);
    this.totalCurrent = this.holdings.reduce((sum, h) => sum + h.currentValue, 0);
    this.totalPnL = this.holdings.reduce((sum, h) => sum + h.pnL, 0);
    this.totalPnLPercent = this.totalInvested > 0 ? (this.totalPnL / this.totalInvested) * 100 : 0;
    
    this.dayPnL = this.holdings.reduce((sum, h) => sum + (h.dayChange * h.quantity), 0);
    this.dayPnLPercent = this.totalCurrent > 0 ? (this.dayPnL / (this.totalCurrent - this.dayPnL)) * 100 : 0;
  }

  analyzeWithGemini(symbol: string): void {
    this.router.navigate(['/chat'], { queryParams: { prompt: `Analyse ${symbol} from my holdings` } });
  }
}
