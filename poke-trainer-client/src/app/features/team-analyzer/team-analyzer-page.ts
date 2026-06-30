import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { TeamAnalyzerService } from './team-analyzer.service';
import { getUserFriendlyErrorMessage } from '../../core/errors/user-friendly-error-message';
import { ErrorState } from '../../shared/components/error-state';
import { PokeballLoader } from '../../shared/components/pokeball-loader';
import { TeamAnalysisResult } from '../../shared/models/dream-team.model';

@Component({
  selector: 'app-team-analyzer-page',
  imports: [RouterLink, PokeballLoader, ErrorState],
  templateUrl: './team-analyzer-page.html',
  styleUrl: './team-analyzer-page.scss'
})
export class TeamAnalyzerPage implements OnInit {
  private readonly analyzerService = inject(TeamAnalyzerService);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly analysis = signal<TeamAnalysisResult | null>(null);

  ngOnInit(): void {
    this.analyze();
  }

  analyze(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.analysis.set(null);

    this.analyzerService.analyze().subscribe({
      next: result => {
        this.analysis.set(result);
        this.isLoading.set(false);
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(getUserFriendlyErrorMessage(error, 'We could not analyze your Dream Team right now. Please try again.'));
      }
    });
  }

  recommendations(report: TeamAnalysisResult): string[] {
    return report.recommendations ?? report.recommendations ?? [];
  }
}
