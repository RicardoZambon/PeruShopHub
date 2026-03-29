import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface OnboardingStep {
  key: string;
  label: string;
  completed: boolean;
}

export interface OnboardingProgress {
  stepsCompleted: string[];
  isCompleted: boolean;
  completedAt: string | null;
  steps: OnboardingStep[];
}

@Injectable({ providedIn: 'root' })
export class OnboardingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/onboarding`;

  getProgress(): Observable<OnboardingProgress> {
    return this.http.get<OnboardingProgress>(this.baseUrl + '/progress');
  }

  completeStep(step: string): Observable<OnboardingProgress> {
    return this.http.post<OnboardingProgress>(this.baseUrl + '/complete-step', { step });
  }
}
