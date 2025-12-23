import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubscriptionService, Plan, UpgradeSubscriptionRequest } from '../../../../core/services/subscription.service';
import { SubscriptionInfo } from '../../../../core/models/subscription.model';
import { CardsManagementComponent } from '../cards-management/cards-management.component';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-subscription-detail',
  standalone: true,
  imports: [CommonModule, CardsManagementComponent],
  templateUrl: './subscription-detail.component.html',
  styleUrl: './subscription-detail.component.css'
})
export class SubscriptionDetailComponent implements OnInit {
  subscription: SubscriptionInfo | null = null;
  plans: Plan[] = [];
  loading = true;
  loadingPlans = false;
  upgrading = false;
  cancelling = false;
  selectedPlan: Plan | null = null;
  selectedBillingCycle: 'Monthly' | 'Yearly' = 'Monthly';
  showUpgradeModal = false;

  constructor(private subscriptionService: SubscriptionService) {}

  ngOnInit(): void {
    this.loadSubscription();
  }

  loadSubscription(): void {
    this.loading = true;
    this.subscriptionService.getCurrentSubscription().subscribe({
      next: (data) => {
        this.subscription = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading subscription:', error);
        this.loading = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'No se pudo cargar la información de la suscripción',
          confirmButtonColor: '#3085d6'
        });
      }
    });
  }

  getStatusBadgeClass(): string {
    if (!this.subscription) return '';
    
    switch (this.subscription.status) {
      case 'Active':
        return 'badge-success';
      case 'Trialing':
        return 'badge-info';
      case 'Expired':
        return 'badge-danger';
      case 'Canceled':
        return 'badge-warning';
      default:
        return 'badge-secondary';
    }
  }

  getStatusText(): string {
    if (!this.subscription) return '';
    
    switch (this.subscription.status) {
      case 'Active':
        return 'Activa';
      case 'Trialing':
        return 'Prueba';
      case 'Expired':
        return 'Expirada';
      case 'Canceled':
        return 'Cancelada';
      default:
        return this.subscription.status;
    }
  }

  getProgressBarClass(percentage: number): string {
    if (percentage >= 90) return 'bg-danger';
    if (percentage >= 75) return 'bg-warning';
    return 'bg-primary';
  }

  openUpgradeModal(): void {
    this.loadingPlans = true;
    this.showUpgradeModal = true;
    
    this.subscriptionService.getAllPlans().subscribe({
      next: (plans) => {
        this.plans = plans.filter(p => p.name !== 'Free');
        this.loadingPlans = false;
      },
      error: (error) => {
        console.error('Error loading plans:', error);
        this.loadingPlans = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'No se pudieron cargar los planes disponibles',
          confirmButtonColor: '#3085d6'
        });
      }
    });
  }

  closeUpgradeModal(): void {
    this.showUpgradeModal = false;
    this.selectedPlan = null;
    this.selectedBillingCycle = 'Monthly';
  }

  selectPlan(plan: Plan): void {
    this.selectedPlan = plan;
  }

  selectBillingCycle(cycle: 'Monthly' | 'Yearly'): void {
    this.selectedBillingCycle = cycle;
  }

  getDisplayPrice(plan: Plan): number {
    if (this.selectedBillingCycle === 'Yearly') {
      return plan.price * 10; // 10 months price for yearly
    }
    return plan.price;
  }

  confirmUpgrade(): void {
    if (!this.selectedPlan) {
      Swal.fire({
        icon: 'warning',
        title: 'Selecciona un plan',
        text: 'Por favor selecciona un plan antes de continuar',
        confirmButtonColor: '#3085d6'
      });
      return;
    }

    Swal.fire({
      title: '¿Confirmar actualización?',
      html: `
        <p>Estás a punto de actualizar a <strong>${this.selectedPlan.displayName}</strong></p>
        <p>Ciclo de facturación: <strong>${this.selectedBillingCycle === 'Monthly' ? 'Mensual' : 'Anual'}</strong></p>
        <p>Precio: <strong>$${this.getDisplayPrice(this.selectedPlan)}</strong></p>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Sí, actualizar',
      cancelButtonText: 'Cancelar'
    }).then((result) => {
      if (result.isConfirmed) {
        this.upgradeSubscription();
      }
    });
  }

  upgradeSubscription(): void {
    if (!this.selectedPlan) return;

    this.upgrading = true;
    const request: UpgradeSubscriptionRequest = {
      newPlanId: this.selectedPlan.id,
      billingCycle: this.selectedBillingCycle
    };

    this.subscriptionService.upgradeSubscription(request).subscribe({
      next: () => {
        this.upgrading = false;
        this.closeUpgradeModal();
        Swal.fire({
          icon: 'success',
          title: '¡Suscripción actualizada!',
          text: 'Tu plan ha sido actualizado exitosamente',
          confirmButtonColor: '#3085d6'
        }).then(() => {
          this.loadSubscription();
        });
      },
      error: (error) => {
        console.error('Error upgrading subscription:', error);
        this.upgrading = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: error.error?.message || 'No se pudo actualizar la suscripción',
          confirmButtonColor: '#3085d6'
        });
      }
    });
  }

  cancelSubscription(): void {
    Swal.fire({
      title: '¿Cancelar suscripción?',
      html: `
        <p>Tu suscripción seguirá activa hasta el final del período actual.</p>
        <p><strong>No se realizarán más cargos automáticos.</strong></p>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Sí, cancelar',
      cancelButtonText: 'No, mantener'
    }).then((result) => {
      if (result.isConfirmed) {
        this.processCancellation();
      }
    });
  }

  processCancellation(): void {
    this.cancelling = true;
    this.subscriptionService.cancelSubscription().subscribe({
      next: () => {
        this.cancelling = false;
        Swal.fire({
          icon: 'success',
          title: 'Suscripción cancelada',
          text: 'Tu suscripción ha sido cancelada. Permanecerá activa hasta el final del período.',
          confirmButtonColor: '#3085d6'
        }).then(() => {
          this.loadSubscription();
        });
      },
      error: (error) => {
        console.error('Error cancelling subscription:', error);
        this.cancelling = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: error.error?.message || 'No se pudo cancelar la suscripción',
          confirmButtonColor: '#3085d6'
        });
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('es-ES', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    });
  }

  getFeaturesList(): string[] {
    if (!this.subscription) return [];
    
    const features: string[] = [];
    const limits = this.subscription.limits;
    
    if (limits.maxDatabases === -1) {
      features.push('Bases de datos ilimitadas');
    } else {
      features.push(`Hasta ${limits.maxDatabases} base(s) de datos`);
    }
    
    if (limits.maxUsers === -1) {
      features.push('Usuarios ilimitados');
    } else {
      features.push(`Hasta ${limits.maxUsers} usuario(s)`);
    }
    
    features.push(`${limits.maxStorageGB} GB de almacenamiento`);
    features.push(`${limits.backupRetentionDays} días de retención`);
    
    if (limits.scheduledBackupsEnabled) features.push('Backups programados');
    if (limits.cloudStorageEnabled) features.push('Almacenamiento en la nube');
    if (limits.apiAccessEnabled) features.push('Acceso a API');
    if (limits.prioritySupport) features.push('Soporte prioritario');
    if (limits.customBrandingEnabled) features.push('Personalización de marca');
    
    return features;
  }

  getExcludedFeaturesList(): string[] {
    if (!this.subscription) return [];
    
    const excludedFeatures: string[] = [];
    const limits = this.subscription.limits;
    
    if (!limits.scheduledBackupsEnabled) excludedFeatures.push('Backups programados');
    if (!limits.cloudStorageEnabled) excludedFeatures.push('Almacenamiento en la nube');
    if (!limits.apiAccessEnabled) excludedFeatures.push('Acceso a API');
    if (!limits.prioritySupport) excludedFeatures.push('Soporte prioritario');
    if (!limits.customBrandingEnabled) excludedFeatures.push('Personalización de marca');
    
    return excludedFeatures;
  }
}
