import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ChatComponent } from './components/chat/chat.component';
import { WatchlistsComponent } from './components/watchlists/watchlists.component';
import { PortfolioComponent } from './components/portfolio/portfolio.component';
import { ResearchComponent } from './components/research/research.component';
import { ThesisComponent } from './components/thesis/thesis.component';
import { RagDocumentsComponent } from './components/rag-documents/rag-documents.component';
import { AuditLogComponent } from './components/audit-log/audit-log.component';
import { IposFuturesComponent } from './components/ipos-futures/ipos-futures.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'chat', component: ChatComponent },
  { path: 'ipos-futures', component: IposFuturesComponent },
  { path: 'ipos', component: IposFuturesComponent },
  { path: 'watchlists', component: WatchlistsComponent },
  { path: 'portfolio', component: PortfolioComponent },
  { path: 'research', component: ResearchComponent },
  { path: 'thesis', component: ThesisComponent },
  { path: 'rag', component: RagDocumentsComponent },
  { path: 'audit-log', component: AuditLogComponent },
  { path: '**', redirectTo: 'dashboard' }
];
