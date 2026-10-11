import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { InvestmentNote, ThesisAlignmentAnalysis } from '../../models/copilot.models';

@Component({
  selector: 'app-thesis',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './thesis.component.html',
  styleUrls: ['./thesis.component.css']
})
export class ThesisComponent implements OnInit {
  theses: InvestmentNote[] = [];
  selectedSymbol: string = 'INFY';
  alignmentReport: ThesisAlignmentAnalysis | null = null;
  isLoadingAlignment = false;

  // New Thesis Modal
  showNewModal = false;
  newSymbol = '';
  newTitle = '';
  newThesisText = '';
  newHorizon = 'Long-Term (3-5 Years)';
  newRisks = '';
  newWatchPoints = '';

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.loadTheses();
  }

  loadTheses(): void {
    this.api.getTheses().subscribe(notes => {
      this.theses = notes;
      if (notes.length > 0 && !this.selectedSymbol) {
        this.selectedSymbol = notes[0].symbol;
      }
      if (this.selectedSymbol) {
        this.runAlignmentAnalysis(this.selectedSymbol);
      }
    });
  }

  runAlignmentAnalysis(symbol: string): void {
    this.selectedSymbol = symbol;
    this.isLoadingAlignment = true;
    this.api.analyzeThesisAlignment(symbol).subscribe({
      next: (report) => {
        this.alignmentReport = report;
        this.isLoadingAlignment = false;
      },
      error: () => this.isLoadingAlignment = false
    });
  }

  get activeThesis(): InvestmentNote | undefined {
    return this.theses.find(t => t.symbol.toUpperCase() === this.selectedSymbol.toUpperCase());
  }

  openCreateThesis(): void {
    this.newSymbol = '';
    this.newTitle = '';
    this.newThesisText = '';
    this.newRisks = '';
    this.newWatchPoints = '';
    this.showNewModal = true;
  }

  submitNewThesis(): void {
    if (!this.newSymbol.trim() || !this.newThesisText.trim()) return;

    this.api.saveThesis({
      symbol: this.newSymbol.trim().toUpperCase(),
      title: this.newTitle.trim() || `${this.newSymbol.toUpperCase()} Core Thesis`,
      thesis: this.newThesisText.trim(),
      timeHorizon: this.newHorizon,
      keyRisks: this.newRisks.trim() || undefined,
      watchPoints: this.newWatchPoints.trim() || undefined
    }).subscribe(() => {
      this.showNewModal = false;
      this.selectedSymbol = this.newSymbol.trim().toUpperCase();
      this.loadTheses();
    });
  }

  askCopilotRegardingThesis(): void {
    this.router.navigate(['/chat'], {
      queryParams: { prompt: `Has anything changed relative to my ${this.selectedSymbol} thesis?` }
    });
  }
}
