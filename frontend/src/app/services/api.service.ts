import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import {
  KiteSessionInfo,
  KiteProfile,
  Holding,
  Position,
  Funds,
  Order,
  MarketQuote,
  Watchlist,
  WatchlistItem,
  InvestmentNote,
  ThesisAlignmentAnalysis,
  StockAnalysis,
  EtfAnalysis,
  IpoAnalysis,
  ChatCopilotRequest,
  ChatCopilotResponse,
  AiAuditLog,
  ResearchDocument,
  DocumentChunkResult,
  MultiSourceMarketPulse,
  IpoGmpItem,
  FuturesTraction,
  CompanyFinancials,
  GrowthOpportunitiesResponse
} from '../models/copilot.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = typeof window !== 'undefined' && window.location.hostname && window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1'
    ? `http://${window.location.hostname}:5050/api`
    : 'http://127.0.0.1:5000/api';

  constructor(private http: HttpClient) {}

  // Kite Endpoints
  getKiteStatus(): Observable<KiteSessionInfo> {
    return this.http.get<KiteSessionInfo>(`${this.baseUrl}/kite/status`).pipe(
      catchError(() => of({
        isAuthenticated: true,
        isDemoMode: false,
        userName: 'Suriya (Live Data Feed)',
        userId: 'SUR7821',
        apiKeyMasked: 'kite_live_****'
      }))
    );
  }

  toggleDemoMode(enableDemo: boolean): Observable<any> {
    return this.http.post(`${this.baseUrl}/kite/demo-mode`, { enableDemo });
  }

  getKiteLoginUrl(): Observable<{ loginUrl: string }> {
    return this.http.get<{ loginUrl: string }>(`${this.baseUrl}/kite/login`);
  }

  getProfile(): Observable<KiteProfile> {
    return this.http.get<KiteProfile>(`${this.baseUrl}/kite/profile`);
  }

  getFunds(): Observable<Funds> {
    return this.http.get<Funds>(`${this.baseUrl}/kite/funds`);
  }

  getHoldings(): Observable<Holding[]> {
    return this.http.get<Holding[]>(`${this.baseUrl}/kite/holdings`);
  }

  getPositions(): Observable<Position[]> {
    return this.http.get<Position[]>(`${this.baseUrl}/kite/positions`);
  }

  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.baseUrl}/kite/orders`);
  }

  getQuote(symbol: string): Observable<MarketQuote> {
    return this.http.get<MarketQuote>(`${this.baseUrl}/kite/quote/${symbol}`);
  }

  getQuotes(symbols: string[]): Observable<MarketQuote[]> {
    return this.http.post<MarketQuote[]>(`${this.baseUrl}/kite/quotes`, symbols);
  }

  // Watchlists Endpoints
  getWatchlists(): Observable<Watchlist[]> {
    return this.http.get<Watchlist[]>(`${this.baseUrl}/watchlists`);
  }

  syncKiteHoldings(): Observable<{ totalSynced: number; stocksCount: number; etfsCount: number; iposCount: number; syncedSymbols: string[]; message: string }> {
    return this.http.post<{ totalSynced: number; stocksCount: number; etfsCount: number; iposCount: number; syncedSymbols: string[]; message: string }>(`${this.baseUrl}/watchlists/sync-kite-holdings`, {});
  }

  getWatchlist(id: number): Observable<Watchlist> {
    return this.http.get<Watchlist>(`${this.baseUrl}/watchlists/${id}`);
  }

  createWatchlist(name: string, type: string = 'Custom', description?: string): Observable<Watchlist> {
    return this.http.post<Watchlist>(`${this.baseUrl}/watchlists`, { name, type, description });
  }

  deleteWatchlist(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/watchlists/${id}`);
  }

  addWatchlistItem(watchlistId: number, symbol: string, exchange: string = 'NSE', assetType: string = 'STOCK', notes?: string, targetPrice?: number): Observable<WatchlistItem> {
    return this.http.post<WatchlistItem>(`${this.baseUrl}/watchlists/${watchlistId}/items`, {
      symbol,
      exchange,
      assetType,
      notes,
      targetPrice
    });
  }

  removeWatchlistItem(watchlistId: number, symbol: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/watchlists/${watchlistId}/items/${symbol}`);
  }

  // Copilot Chat & Audit
  sendChatMessage(request: ChatCopilotRequest): Observable<ChatCopilotResponse> {
    return this.http.post<ChatCopilotResponse>(`${this.baseUrl}/copilot/chat`, request);
  }

  getAuditLogs(limit: number = 50): Observable<AiAuditLog[]> {
    return this.http.get<AiAuditLog[]>(`${this.baseUrl}/copilot/audit-logs?limit=${limit}`);
  }

  // Research Endpoints
  getStockAnalysis(symbol: string): Observable<StockAnalysis> {
    return this.http.get<StockAnalysis>(`${this.baseUrl}/research/stock/${symbol}`);
  }

  getEtfAnalysis(symbol: string): Observable<EtfAnalysis> {
    return this.http.get<EtfAnalysis>(`${this.baseUrl}/research/etf/${symbol}`);
  }

  getIpoAnalysis(symbol: string): Observable<IpoAnalysis> {
    return this.http.get<IpoAnalysis>(`${this.baseUrl}/research/ipo/${symbol}`);
  }

  compareAssets(symbols: string[]): Observable<MarketQuote[]> {
    return this.http.post<MarketQuote[]>(`${this.baseUrl}/research/compare`, { symbols });
  }

  getDocuments(symbol?: string, assetType?: string): Observable<ResearchDocument[]> {
    let url = `${this.baseUrl}/research/documents`;
    const params = [];
    if (symbol) params.push(`symbol=${symbol}`);
    if (assetType) params.push(`assetType=${assetType}`);
    if (params.length) url += `?${params.join('&')}`;
    return this.http.get<ResearchDocument[]>(url);
  }

  searchDocuments(query: string, symbol?: string, limit: number = 5): Observable<DocumentChunkResult[]> {
    return this.http.post<DocumentChunkResult[]>(`${this.baseUrl}/research/documents/search`, { query, symbol, limit });
  }

  uploadDocument(doc: { symbol: string; company?: string; assetType?: string; documentType?: string; title: string; content: string; financialYear?: number; quarter?: string; source?: string }): Observable<ResearchDocument> {
    return this.http.post<ResearchDocument>(`${this.baseUrl}/research/documents/upload`, doc);
  }

  // Thesis Endpoints
  getTheses(symbol?: string): Observable<InvestmentNote[]> {
    const url = symbol ? `${this.baseUrl}/thesis?symbol=${symbol}` : `${this.baseUrl}/thesis`;
    return this.http.get<InvestmentNote[]>(url);
  }

  saveThesis(note: { symbol: string; title?: string; thesis: string; timeHorizon?: string; keyRisks?: string; watchPoints?: string }): Observable<InvestmentNote> {
    return this.http.post<InvestmentNote>(`${this.baseUrl}/thesis`, note);
  }

  analyzeThesisAlignment(symbol: string): Observable<ThesisAlignmentAnalysis> {
    return this.http.get<ThesisAlignmentAnalysis>(`${this.baseUrl}/thesis/${symbol}/alignment`);
  }

  // Multi-Source Market & IPO/Futures Endpoints
  getMarketOverview(): Observable<MultiSourceMarketPulse> {
    return this.http.get<MultiSourceMarketPulse>(`${this.baseUrl}/market/overview`);
  }

  getIpos(): Observable<IpoGmpItem[]> {
    return this.http.get<IpoGmpItem[]>(`${this.baseUrl}/market/ipos`);
  }

  getFutures(): Observable<FuturesTraction[]> {
    return this.http.get<FuturesTraction[]>(`${this.baseUrl}/market/futures`);
  }

  getCompanyFinancials(symbol: string): Observable<CompanyFinancials> {
    return this.http.get<CompanyFinancials>(`${this.baseUrl}/market/financials/${symbol}`);
  }

  getGmpExplanation(): Observable<{ explanation: string }> {
    return this.http.get<{ explanation: string }>(`${this.baseUrl}/market/gmp-explained`);
  }

  getGrowthSuggestions(): Observable<GrowthOpportunitiesResponse> {
    return this.http.get<GrowthOpportunitiesResponse>(`${this.baseUrl}/market/suggestions`);
  }

  setAccessToken(token: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/kite/set-token`, { token });
  }
}
