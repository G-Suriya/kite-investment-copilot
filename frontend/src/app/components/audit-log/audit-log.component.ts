import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AiAuditLog } from '../../models/copilot.models';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audit-log.component.html',
  styleUrls: ['./audit-log.component.css']
})
export class AuditLogComponent implements OnInit {
  logs: AiAuditLog[] = [];
  isLoading = true;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading = true;
    this.api.getAuditLogs(100).subscribe({
      next: (data) => {
        this.logs = data;
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }
}
