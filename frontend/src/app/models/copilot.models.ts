export interface KiteSessionInfo {
  isAuthenticated: boolean;
  isDemoMode: boolean;
  userName?: string;
  userId?: string;
  loginTime?: string;
  apiKeyMasked: string;
}

export interface KiteProfile {
  userId: string;
  userName: string;
  userType: string;
  email: string;
  broker: string;
  isConnected: boolean;
  connectedAt?: string;
}

export interface Holding {
  symbol: string;
  exchange: string;
  instrumentToken: string;
  quantity: number;
  t1Quantity: number;
  averagePrice: number;
  lastPrice: number;
  closePrice: number;
  pnL: number;
  pnLPercent: number;
  dayChange: number;
  dayChangePercent: number;
  currentValue: number;
  investedValue: number;
}

export interface Position {
  symbol: string;
  exchange: string;
  product: string;
  quantity: number;
  buyQuantity: number;
  sellQuantity: number;
  buyPrice: number;
  sellPrice: number;
  lastPrice: number;
  pnL: number;
  value: number;
}

export interface Funds {
  availableMargin: number;
  usedMargin: number;
  cashBalance: number;
  collateralMargin: number;
  totalMargin: number;
  currency: string;
}

export interface Order {
  orderId: string;
  symbol: string;
  exchange: string;
  transactionType: string;
  orderType: string;
  product: string;
  quantity: number;
  price: number;
  status: string;
  statusMessage?: string;
  orderTimestamp: string;
}

export interface MarketQuote {
  symbol: string;
  exchange: string;
  companyName: string;
  lastPrice: number;
  change: number;
  changePercent: number;
  open: number;
  high: number;
  low: number;
  previousClose: number;
  volume: number;
  peRatio?: number;
  pbRatio?: number;
  marketCapCrores?: number;
  fiftyTwoWeekHigh: number;
  fiftyTwoWeekLow: number;
  dividendYield?: number;
  sector?: string;
  timestamp: string;
}

export interface Watchlist {
  id: number;
  name: string;
  type: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  items: WatchlistItem[];
}

export interface WatchlistItem {
  id: number;
  watchlistId: number;
  symbol: string;
  exchange: string;
  assetType: 'STOCK' | 'ETF' | 'IPO' | 'INDEX';
  companyName?: string;
  notes?: string;
  targetPrice?: number;
  addedAt: string;
}

export interface InvestmentNote {
  id: number;
  symbol: string;
  title: string;
  content: string;
  thesis: string;
  timeHorizon: string;
  keyRisks?: string;
  watchPoints?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ThesisAlignmentAnalysis {
  symbol: string;
  companyName: string;
  originalThesis: string;
  timeHorizon: string;
  currentPrice: number;
  targetPrice?: number;
  confirmatoryFactors: string[];
  contradictoryOrRiskFactors: string[];
  developmentsSinceThesis: string[];
  keyQuestionsToInvestigate: string[];
  overallAssessment: string;
}

export interface StockAnalysis {
  symbol: string;
  companyName: string;
  quote: MarketQuote;
  currentHolding?: Holding;
  userThesis?: InvestmentNote;
  keyFilingExcerpts: DocumentChunkResult[];
  businessSummary: string;
  financialHighlights: Record<string, string>;
  risksAndWatchlistPoints: string[];
}

export interface EtfAnalysis {
  symbol: string;
  name: string;
  underlyingIndex: string;
  quote: MarketQuote;
  expenseRatio: number;
  trackingError: number;
  aumCrores: number;
  topHoldings: string[];
  sectorDistribution: string;
  thesisSuitability: string;
  watchPoints: string[];
}

export interface IpoAnalysis {
  symbol: string;
  companyName: string;
  issuePeriod: string;
  priceBand: string;
  issueSizeCrores: number;
  freshIssueCrores: number;
  ofsCrores: number;
  postIssuePe: string;
  promoterHoldingPrePost: string;
  strengths: string[];
  keyRisks: string[];
  status: string;
}

export interface DocumentChunkResult {
  documentId: number;
  symbol: string;
  company: string;
  documentType: string;
  title: string;
  content: string;
  similarityScore: number;
  quarter?: string;
  year?: number;
  source: string;
}

export interface ResearchDocument {
  id: number;
  symbol: string;
  company: string;
  assetType: string;
  documentType: string;
  financialYear?: number;
  quarter?: string;
  publishedDate: string;
  source: string;
  title: string;
  summary?: string;
  content?: string;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  toolCalls?: ToolCallExecutionResult[];
  guardrailTriggered?: boolean;
  guardrailMessage?: string;
  suggestedFollowUps?: string[];
  beginnerSummary?: string;
  proAnalysis?: string;
  activeView?: 'all' | 'beginner' | 'pro';
}

export interface ChatCopilotRequest {
  message: string;
  conversationId?: string;
  experienceLevel?: string;
  history?: { role: string; content: string }[];
}

export interface ChatCopilotResponse {
  response: string;
  conversationId: string;
  beginnerSummary?: string;
  proAnalysis?: string;
  experienceLevel?: string;
  toolCallsExecuted: ToolCallExecutionResult[];
  suggestedFollowUps: string[];
  guardrailTriggered: boolean;
  guardrailMessage?: string;
}

export interface ToolCallExecutionResult {
  toolName: string;
  arguments?: any;
  result?: any;
  success: boolean;
  errorMessage?: string;
  executionTimeMs: number;
}

export interface AiAuditLog {
  id: number;
  timestamp: string;
  user: string;
  conversationId: string;
  prompt: string;
  tool?: string;
  arguments?: string;
  result?: string;
  success: boolean;
  executionMs: number;
}

export interface IpoGmpItem {
  symbol: string;
  companyName: string;
  priceBand: string;
  issuePrice: number;
  gmp: number;
  gmpPercent: number;
  expectedListingPrice: number;
  gmpTrend: string;
  rating: string;
  lotSize: number;
  issueSizeCrores: number;
  subscriptionTimes: number;
  openDate: string;
  closeDate: string;
  listingDate: string;
  status: string;
  sector: string;
  gmpExplanationSimple: string;
  ipoType?: string;
  docUrl?: string;
  logoColor?: string;
  logoInitials?: string;
  openingDateDisplay?: string;
  stage?: 'UPCOMING' | 'OPEN' | 'CLOSED' | 'SUBSCRIPTION' | 'ALLOTMENT' | string;
  registrar?: string;
  registrarUrl?: string;
  minInvestment?: number;
  qibSubscriptionTimes?: number;
  niiSubscriptionTimes?: number;
  retailSubscriptionTimes?: number;
  allotmentDate?: string;
}

export interface FuturesTraction {
  symbol: string;
  expiryDate: string;
  spotPrice: number;
  futuresPrice: number;
  basisPremiumDiscount: number;
  openInterest: number;
  openInterestChangePercent: number;
  buildupType: string;
  buildupDescription: string;
  putCallRatio: number;
  pcrSentiment: string;
  maxPain: number;
  volumeContracts: number;
}

export interface MarketIndex {
  name: string;
  value: number;
  change: number;
  changePercent: number;
  exchange: string;
  high: number;
  low: number;
}

export interface MarketBreadth {
  advances: number;
  declines: number;
  unchanged: number;
  advanceDeclineRatio: number;
}

export interface InstitutionalFlows {
  fiiNetCrores: number;
  diiNetCrores: number;
  netTotalCrores: number;
  date: string;
  interpretation: string;
}

export interface StockMover {
  symbol: string;
  companyName: string;
  lastPrice: number;
  changePercent: number;
  volumeCrores: number;
  sector: string;
}

export interface SectorPerformance {
  sectorName: string;
  changePercent: number;
  topStock: string;
  trend: string;
}

export interface MultiSourceMarketPulse {
  majorIndices: MarketIndex[];
  marketBreadth: MarketBreadth;
  institutionalFlows: InstitutionalFlows;
  topGainers: StockMover[];
  topLosers: StockMover[];
  sectorPerformance: SectorPerformance[];
  marketSummaryText: string;
  timestamp: string;
}

export interface QuarterlyFinancial {
  quarter: string;
  year: number;
  revenueCrores: number;
  ebitdaCrores: number;
  netProfitCrores: number;
  operatingMarginPercent: number;
  yoYRevenueGrowthPercent: number;
  yoYProfitGrowthPercent: number;
}

export interface AnnualFinancial {
  year: string;
  revenueCrores: number;
  netProfitCrores: number;
  ebitdaCrores: number;
  operatingMarginPercent: number;
}

export interface CompanyFinancials {
  symbol: string;
  companyName: string;
  sector: string;
  marketCapCrores: number;
  peRatio?: number;
  pbRatio?: number;
  roePercent?: number;
  rocePercent?: number;
  dividendYieldPercent?: number;
  currentPrice: number;
  dayChangePercent: number;
  quarterlyResults: QuarterlyFinancial[];
  annualResults: AnnualFinancial[];
  whyPriceIsRising: string[];
  whyPriceIsFalling: string[];
  summaryVerdict: string;
}

export interface StockGrowthOpportunity {
  symbol: string;
  companyName: string;
  sector: string;
  currentPrice: number;
  targetPrice: number;
  upsidePotentialPercent: number;
  peRatio?: number;
  roePercent?: number;
  rocePercent?: number;
  yoYRevenueGrowthPercent: number;
  yoYProfitGrowthPercent: number;
  growthTheme: string;
  fullAnalysis: string;
  catalysts: string[];
  risks: string[];
  recommendation: string;
  technicalSetup: string;
  timeHorizon: string;
}

export interface IpoGrowthOpportunity {
  symbol: string;
  companyName: string;
  sector: string;
  priceBand: string;
  issuePrice: number;
  issueSizeCrores: number;
  gmp: number;
  gmpPercent: number;
  expectedListingPrice: number;
  subscriptionTimes: number;
  status: string;
  recommendation: string;
  yoYRevenueGrowthPercent: number;
  financialHealthSummary: string;
  fullAnalysis: string;
  pros: string[];
  cons: string[];
}

export interface GrowthOpportunitiesResponse {
  topIpos: IpoGrowthOpportunity[];
  topStocks: StockGrowthOpportunity[];
  marketContext: string;
  lastUpdatedAt: string;
}

