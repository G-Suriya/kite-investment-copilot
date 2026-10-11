import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ApiService } from './services/api.service';
import { KiteSessionInfo, Funds } from './models/copilot.models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit {
  title = 'kite-copilot-frontend';
  session: KiteSessionInfo = { isAuthenticated: true, isDemoMode: false, apiKeyMasked: 'kite_live_****' };
  funds: Funds = { availableMargin: 0, usedMargin: 0, cashBalance: 0, collateralMargin: 0, totalMargin: 0, currency: 'INR' };

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    if (typeof document !== 'undefined' && document.body) {
      document.body.className = '';
    }
    try {
      localStorage.removeItem('kite_copilot_theme');
    } catch {}

    this.refreshLiveStatus();
    // Continuous background live sync every 25 seconds
    setInterval(() => {
      this.refreshLiveStatus();
    }, 25000);
  }

  refreshLiveStatus(): void {
    this.api.getKiteStatus().subscribe(s => this.session = s);
    this.api.getFunds().subscribe(f => this.funds = f);
  }

  toggleDemo(): void {
    this.refreshLiveStatus();
  }

  quickChat(): void {
    this.router.navigate(['/chat']);
  }
}

