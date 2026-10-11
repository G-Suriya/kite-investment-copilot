import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ResearchDocument, DocumentChunkResult } from '../../models/copilot.models';

@Component({
  selector: 'app-rag-documents',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rag-documents.component.html',
  styleUrls: ['./rag-documents.component.css']
})
export class RagDocumentsComponent implements OnInit {
  documents: ResearchDocument[] = [];
  searchResults: DocumentChunkResult[] = [];
  searchQuery: string = 'Topaz generative AI deal pipeline and margins';
  isSearching = false;
  isLoadingDocs = true;

  // Ingest Modal
  showUploadModal = false;
  uploadSymbol = '';
  uploadCompany = '';
  uploadTitle = '';
  uploadContent = '';
  uploadDocType = 'QuarterlyResult';
  uploadYear = 2026;
  uploadQuarter = 'Q2';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadDocuments();
    this.runSemanticSearch();
  }

  loadDocuments(): void {
    this.isLoadingDocs = true;
    this.api.getDocuments().subscribe({
      next: (docs) => {
        this.documents = docs;
        this.isLoadingDocs = false;
      },
      error: () => this.isLoadingDocs = false
    });
  }

  runSemanticSearch(): void {
    if (!this.searchQuery.trim()) return;

    this.isSearching = true;
    this.api.searchDocuments(this.searchQuery.trim(), undefined, 6).subscribe({
      next: (res) => {
        this.searchResults = res;
        this.isSearching = false;
      },
      error: () => this.isSearching = false
    });
  }

  openUploadModal(): void {
    this.uploadSymbol = '';
    this.uploadCompany = '';
    this.uploadTitle = '';
    this.uploadContent = '';
    this.showUploadModal = true;
  }

  submitUpload(): void {
    if (!this.uploadSymbol.trim() || !this.uploadTitle.trim() || !this.uploadContent.trim()) return;

    this.api.uploadDocument({
      symbol: this.uploadSymbol.trim().toUpperCase(),
      company: this.uploadCompany.trim() || this.uploadSymbol.toUpperCase(),
      title: this.uploadTitle.trim(),
      content: this.uploadContent.trim(),
      documentType: this.uploadDocType,
      financialYear: this.uploadYear,
      quarter: this.uploadQuarter,
      source: 'User Upload'
    }).subscribe(() => {
      this.showUploadModal = false;
      this.loadDocuments();
    });
  }
}
