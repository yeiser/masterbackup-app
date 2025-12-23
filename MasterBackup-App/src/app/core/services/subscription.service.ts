import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SubscriptionInfo } from '../models/subscription.model';

export interface Plan {
  id: string;
  name: string;
  displayName: string;
  description: string;
  price: number;
  billingCycle: string;
  maxDatabases: number;
  maxUsers: number;
  maxStorageGB: number;
  backupRetentionDays: number;
  features: string[];
  isPopular: boolean;
}

export interface CreateSubscriptionRequest {
  planId: string;
  billingCycle: string;
  isTrial?: boolean;
}

export interface UpgradeSubscriptionRequest {
  newPlanId: string;
  billingCycle: string;
}

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
  private apiUrl = `${environment.apiUrl}/subscriptions`;
  private plansUrl = `${environment.apiUrl}/plans`;

  constructor(private http: HttpClient) {}

  getCurrentSubscription(): Observable<SubscriptionInfo> {
    return this.http.get<SubscriptionInfo>(`${this.apiUrl}/current`);
  }

  getAllPlans(): Observable<Plan[]> {
    return this.http.get<Plan[]>(this.plansUrl);
  }

  getPlanById(id: string): Observable<Plan> {
    return this.http.get<Plan>(`${this.plansUrl}/${id}`);
  }

  createSubscription(request: CreateSubscriptionRequest): Observable<any> {
    return this.http.post(this.apiUrl, request);
  }

  upgradeSubscription(request: UpgradeSubscriptionRequest): Observable<any> {
    return this.http.put(`${this.apiUrl}/upgrade`, request);
  }

  cancelSubscription(): Observable<any> {
    return this.http.post(`${this.apiUrl}/cancel`, {});
  }
}
