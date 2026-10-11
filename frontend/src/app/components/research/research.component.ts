import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { StockAnalysis, EtfAnalysis, IpoAnalysis, MarketQuote } from '../../models/copilot.models';

@Component({
  selector: 'app-research',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './research.component.html',
  styleUrls: ['./research.component.css']
})
export class ResearchComponent implements OnInit {
  activeView: 'stock' | 'etf' | 'ipo' | 'compare' = 'stock';

  // Stock
  selectedStockSymbol = 'INFY';
  stockData: StockAnalysis | null = null;
  stockLoading = false;

  // ETF
  selectedEtfSymbol = 'MON100';
  etfData: EtfAnalysis | null = null;
  etfLoading = false;

  // IPO
  selectedIpoSymbol = 'SWIGGY';
  ipoData: IpoAnalysis | null = null;
  ipoLoading = false;

  // Compare
  compareInput = 'INFY, TCS, HDFCBANK';
  comparedQuotes: MarketQuote[] = [];
  compareLoading = false;

  stockList = ['INFY', 'TCS', 'HDFCBANK', 'RELIANCE', 'TATAMOTORS'];
  etfList = ['MON100', 'NIFTYBEES', 'GOLDBEES'];
  ipoList = ['SWIGGY', 'HYUNDAI', 'NTPCGREEN'];

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.loadStock(this.selectedStockSymbol);
    this.loadEtf(this.selectedEtfSymbol);
    this.loadIpo(this.selectedIpoSymbol);
    this.runCompare();
  }

  loadStock(symbol: string): void {
    this.selectedStockSymbol = symbol;
    this.stockLoading = true;
    this.api.getStockAnalysis(symbol).subscribe({
      next: (data) => {
        this.stockData = data;
        this.stockLoading = false;
      },
      error: () => this.stockLoading = false
    });
  }

  loadEtf(symbol: string): void {
    this.selectedEtfSymbol = symbol;
    this.etfLoading = true;
    this.api.getEtfAnalysis(symbol).subscribe({
      next: (data) => {
        this.etfData = data;
        this.etfLoading = false;
      },
      error: () => this.etfLoading = false
    });
  }

  loadIpo(symbol: string): void {
    this.selectedIpoSymbol = symbol;
    this.ipoLoading = true;
    this.api.getIpoAnalysis(symbol).subscribe({
      next: (data) => {
        this.ipoData = data;
        this.ipoLoading = false;
      },
      error: () => this.ipoLoading = false
    });
  }

  runCompare(): void {
    const symbols = this.compareInput.split(',').map(s => s.trim().toUpperCase()).filter(s => !!s);
    if (symbols.length === 0) return;

    this.compareLoading = true;
    this.api.compareAssets(symbols).subscribe({
      next: (quotes) => {
        this.comparedQuotes = quotes;
        this.compareLoading = false;
      },
      error: () => this.compareLoading = false
    });
  }

  askCopilot(prompt: string): void {
    this.router.navigate(['/chat'], { queryParams: { prompt } });
  }
}
