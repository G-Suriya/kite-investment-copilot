import { Component, OnInit, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { ChatMessage, ToolCallExecutionResult } from '../../models/copilot.models';
import { marked } from 'marked';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  messages: ChatMessage[] = [];
  userInput: string = '';
  isLoading: boolean = false;
  conversationId: string = 'conv_' + Math.random().toString(36).substring(2, 9);
  copiedMessageId: string | null = null;
  experienceMode: 'all' | 'beginner' | 'pro' = 'all';
  private shouldScroll = false;

  beginnerFilters = [
    { label: '🌟 Suggest Growth Stocks & IPOs', prompt: 'suggest new stocks or new IPO based GMP and Stocks and company growth with full analys' },
    { label: '🌱 Where Do I Start?', prompt: 'I am a newbie where do I start investing in India?' },
    { label: '💡 What is P/E Ratio?', prompt: 'What is P/E ratio in simple terms with an everyday example?' },
    { label: '🎯 What is NIFTY 50?', prompt: 'What is NIFTY 50 and how does an index work?' },
    { label: '🚀 What is GMP?', prompt: 'What is GMP in simple terms with an example?' },
    { label: '⚠️ Delivery vs Intraday', prompt: 'What is the difference between CNC Delivery and MIS Intraday in Zerodha?' },
    { label: '💼 My Holdings & P&L', prompt: 'Show my Zerodha holdings and overall P&L' }
  ];

  proFilters = [
    { label: '🌟 Top Growth Compounders & IPOs', prompt: 'suggest new stocks or new IPO based GMP and Stocks and company growth with full analys' },
    { label: '⚡ TCS Technicals & Pivots', prompt: 'Show TCS technical levels, RSI, and pivot points' },
    { label: '📈 Nifty F&O Traction & PCR', prompt: 'Show Nifty futures and derivatives open interest traction' },
    { label: '📉 Why TCS Falling vs Rising?', prompt: 'Why is TCS price going up or falling? Explain catalysts and headwinds' },
    { label: '📊 Company Revenue & Margins', prompt: 'Show TCS quarterly revenue and margin trajectory' },
    { label: '⚖️ Compare TCS vs INFY', prompt: 'Compare TCS and INFY valuation, P/E, and margins' },
    { label: '🎯 INFY Thesis Alignment', prompt: 'Check my investment thesis alignment for INFY' }
  ];

  adaptiveFilters = [
    { label: '🌟 Suggest Stocks & High-GMP IPOs', prompt: 'suggest new stocks or new IPO based GMP and Stocks and company growth with full analys' },
    { label: '🚀 Live IPOs & GMP', prompt: 'Show live Indian IPO pipeline with grey market premium (GMP)' },
    { label: '💡 What is GMP?', prompt: 'What is GMP in simple terms with an example?' },
    { label: '📉 Why Falling / Rising?', prompt: 'Why is HDFCBANK falling recently? Explain catalysts and headwinds' },
    { label: '⚡ Technicals & RSI', prompt: 'Show TCS technical levels, RSI, and pivot points' },
    { label: '📊 Company Revenue', prompt: 'Show TCS quarterly revenue and margin trajectory' },
    { label: '💼 My Holdings', prompt: 'Show my Zerodha holdings and overall P&L' }
  ];

  get currentFilters() {
    if (this.experienceMode === 'beginner') return this.beginnerFilters;
    if (this.experienceMode === 'pro') return this.proFilters;
    return this.adaptiveFilters;
  }

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.messages.push({
      id: 'msg_welcome',
      role: 'assistant',
      content: `### ✦ Kite Copilot: Adaptive Intelligence (Newbie to Pro)

Connected live to **Zerodha Kite**, **NSE / BSE**, and multi-broker telemetry (Groww, INDmoney, Moneycontrol).

* **🌱 Beginner / Newbie Friendly**: Plain-English explanations (ELI5), real-life analogies (chai stall metaphors), and safety guardrails.
* **⚡ Pro Quantitative Terminal**: Institutional metrics (P/E, ROCE, PEG), Technical Setups (20 EMA, 50/200 SMA, RSI, Pivot Points), and Derivatives F&O Traction (Open Interest & PCR).
* **🎯 Experience Switcher**: Toggle between **Beginner (ELI5)**, **Pro Trader**, or **Adaptive Dual-Mode** anytime using the toolbar above!

*Type any query below or click any one-click trigger:*`,
      timestamp: new Date(),
      activeView: 'all',
      suggestedFollowUps: [
        'I am a newbie where do I start?',
        'What is P/E ratio in simple terms?',
        'Show TCS technical levels and pivot points',
        'Show live IPO pipeline and GMP'
      ]
    });

    this.route.queryParams.subscribe(params => {
      const incomingPrompt = params['prompt'] || params['message'] || params['q'];
      if (incomingPrompt && typeof incomingPrompt === 'string' && incomingPrompt.trim().length > 0) {
        const text = incomingPrompt.trim();
        // Clean queryParams in URL so refreshing doesn't duplicate
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: {},
          replaceUrl: true
        });
        // Auto trigger the message
        setTimeout(() => {
          this.sendMessage(text);
        }, 150);
      }
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  setExperienceMode(mode: 'all' | 'beginner' | 'pro'): void {
    this.experienceMode = mode;
  }

  setMessageView(msg: ChatMessage, view: 'all' | 'beginner' | 'pro'): void {
    msg.activeView = view;
  }

  getDisplayContent(msg: ChatMessage): string {
    if (msg.activeView === 'beginner' && msg.beginnerSummary) {
      return msg.beginnerSummary;
    }
    if (msg.activeView === 'pro' && msg.proAnalysis) {
      return msg.proAnalysis;
    }
    return msg.content;
  }

  sendMessage(text?: string): void {
    const prompt = (text || this.userInput).trim();
    if (!prompt || this.isLoading) return;

    this.messages.push({
      id: 'msg_' + Date.now(),
      role: 'user',
      content: prompt,
      timestamp: new Date()
    });

    this.userInput = '';
    this.isLoading = true;
    this.shouldScroll = true;

    const history = this.messages.slice(-6).map(m => ({
      role: m.role,
      content: m.content
    }));

    this.api.sendChatMessage({
      message: prompt,
      conversationId: this.conversationId,
      experienceLevel: this.experienceMode,
      history: history
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.conversationId = res.conversationId || this.conversationId;

        // Auto-extract beginner and pro sections if not pre-split
        let beg = res.beginnerSummary;
        let pro = res.proAnalysis;
        if (!beg && (res.response.includes('#### 🌱') || res.response.includes('#### 📚 Concept Guide:'))) {
          const parts = res.response.split(/#### ⚡|#### 🎯/);
          if (parts.length > 0) beg = parts[0];
          if (parts.length > 1) pro = '#### ⚡ ' + parts.slice(1).join('\n\n#### ⚡ ');
        }

        this.messages.push({
          id: 'msg_' + Date.now(),
          role: 'assistant',
          content: res.response,
          beginnerSummary: beg,
          proAnalysis: pro,
          activeView: this.experienceMode === 'beginner' && beg ? 'beginner' : (this.experienceMode === 'pro' && pro ? 'pro' : 'all'),
          timestamp: new Date(),
          toolCalls: res.toolCallsExecuted,
          guardrailTriggered: res.guardrailTriggered,
          guardrailMessage: res.guardrailMessage,
          suggestedFollowUps: res.suggestedFollowUps
        });
        this.shouldScroll = true;
      },
      error: (err) => {
        this.isLoading = false;
        this.messages.push({
          id: 'msg_' + Date.now(),
          role: 'assistant',
          content: `⚠️ **Connection Error**: Could not reach Kite Copilot API.\n\n*Details: ${err.message || 'Network error'}*`,
          timestamp: new Date()
        });
        this.shouldScroll = true;
      }
    });
  }

  copyContent(content: string, id: string): void {
    navigator.clipboard.writeText(content);
    this.copiedMessageId = id;
    setTimeout(() => {
      if (this.copiedMessageId === id) this.copiedMessageId = null;
    }, 2000);
  }

  renderMarkdown(content: string): string {
    try {
      return marked.parse(content) as string;
    } catch {
      return content;
    }
  }

  private scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    } catch {}
  }
}
