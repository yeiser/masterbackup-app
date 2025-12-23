import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';

export interface CreditCard {
  id: string;
  cardholderName: string;
  cardNumber: string;
  lastFourDigits: string;
  expiryMonth: string;
  expiryYear: string;
  cardType: 'visa' | 'mastercard' | 'amex' | 'discover';
  isDefault: boolean;
  createdAt: Date;
}

@Component({
  selector: 'app-cards-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cards-management.component.html',
  styleUrl: './cards-management.component.css'
})
export class CardsManagementComponent implements OnInit {
  cards: CreditCard[] = [];
  showAddCardModal = false;
  
  // Form fields
  cardholderName = '';
  cardNumber = '';
  expiryMonth = '';
  expiryYear = '';
  cvv = '';
  setAsDefault = false;
  
  // Form validation
  submitted = false;
  adding = false;

  ngOnInit(): void {
    this.loadMockCards();
  }

  loadMockCards(): void {
    // Mock data for demonstration
    this.cards = [
      {
        id: '1',
        cardholderName: 'Juan Pérez',
        cardNumber: '4532********1234',
        lastFourDigits: '1234',
        expiryMonth: '12',
        expiryYear: '2026',
        cardType: 'visa',
        isDefault: true,
        createdAt: new Date('2023-01-15')
      },
      {
        id: '2',
        cardholderName: 'María García',
        cardNumber: '5425********5678',
        lastFourDigits: '5678',
        expiryMonth: '08',
        expiryYear: '2025',
        cardType: 'mastercard',
        isDefault: false,
        createdAt: new Date('2023-06-20')
      }
    ];
  }

  openAddCardModal(): void {
    this.showAddCardModal = true;
    this.resetForm();
  }

  closeAddCardModal(): void {
    this.showAddCardModal = false;
    this.resetForm();
  }

  resetForm(): void {
    this.cardholderName = '';
    this.cardNumber = '';
    this.expiryMonth = '';
    this.expiryYear = '';
    this.cvv = '';
    this.setAsDefault = false;
    this.submitted = false;
  }

  formatCardNumber(): void {
    // Remove all non-digits
    let cleaned = this.cardNumber.replace(/\D/g, '');
    
    // Limit to 16 digits
    cleaned = cleaned.substring(0, 16);
    
    // Format with spaces every 4 digits
    const formatted = cleaned.match(/.{1,4}/g)?.join(' ') || '';
    this.cardNumber = formatted;
  }

  formatExpiry(): void {
    // Ensure month is 2 digits
    if (this.expiryMonth.length === 1 && parseInt(this.expiryMonth) > 1) {
      this.expiryMonth = '0' + this.expiryMonth;
    }
  }

  detectCardType(cardNumber: string): 'visa' | 'mastercard' | 'amex' | 'discover' {
    const cleaned = cardNumber.replace(/\D/g, '');
    
    if (cleaned.startsWith('4')) return 'visa';
    if (cleaned.startsWith('5')) return 'mastercard';
    if (cleaned.startsWith('34') || cleaned.startsWith('37')) return 'amex';
    if (cleaned.startsWith('6011') || cleaned.startsWith('65')) return 'discover';
    
    return 'visa'; // default
  }

  validateForm(): boolean {
    if (!this.cardholderName.trim()) return false;
    if (this.cardNumber.replace(/\D/g, '').length !== 16) return false;
    if (!this.expiryMonth || parseInt(this.expiryMonth) < 1 || parseInt(this.expiryMonth) > 12) return false;
    if (!this.expiryYear || this.expiryYear.length !== 4) return false;
    if (this.cvv.length < 3 || this.cvv.length > 4) return false;
    
    // Check if card is not expired
    const currentYear = new Date().getFullYear();
    const currentMonth = new Date().getMonth() + 1;
    const expYear = parseInt(this.expiryYear);
    const expMonth = parseInt(this.expiryMonth);
    
    if (expYear < currentYear || (expYear === currentYear && expMonth < currentMonth)) {
      return false;
    }
    
    return true;
  }

  addCard(): void {
    this.submitted = true;
    
    if (!this.validateForm()) {
      Swal.fire({
        icon: 'error',
        title: 'Datos inválidos',
        text: 'Por favor verifica que todos los campos sean correctos',
        confirmButtonColor: '#3085d6'
      });
      return;
    }

    this.adding = true;

    // Simulate API call
    setTimeout(() => {
      const cleanedNumber = this.cardNumber.replace(/\D/g, '');
      const lastFour = cleanedNumber.slice(-4);
      const maskedNumber = cleanedNumber.slice(0, 4) + '********' + lastFour;

      const newCard: CreditCard = {
        id: Date.now().toString(),
        cardholderName: this.cardholderName,
        cardNumber: maskedNumber,
        lastFourDigits: lastFour,
        expiryMonth: this.expiryMonth,
        expiryYear: this.expiryYear,
        cardType: this.detectCardType(cleanedNumber),
        isDefault: this.setAsDefault,
        createdAt: new Date()
      };

      // If setting as default, remove default from other cards
      if (this.setAsDefault) {
        this.cards.forEach(card => card.isDefault = false);
      }

      this.cards.push(newCard);
      this.adding = false;
      this.closeAddCardModal();

      Swal.fire({
        icon: 'success',
        title: '¡Tarjeta agregada!',
        text: 'La tarjeta ha sido agregada exitosamente',
        confirmButtonColor: '#3085d6',
        timer: 2000
      });
    }, 1500);
  }

  setDefaultCard(card: CreditCard): void {
    Swal.fire({
      title: '¿Establecer como predeterminada?',
      text: `¿Deseas usar la tarjeta terminada en ${card.lastFourDigits} como método de pago predeterminado?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Sí, establecer',
      cancelButtonText: 'Cancelar'
    }).then((result) => {
      if (result.isConfirmed) {
        this.cards.forEach(c => c.isDefault = false);
        card.isDefault = true;

        Swal.fire({
          icon: 'success',
          title: '¡Actualizado!',
          text: 'Tarjeta predeterminada actualizada',
          confirmButtonColor: '#3085d6',
          timer: 2000
        });
      }
    });
  }

  deleteCard(card: CreditCard): void {
    if (card.isDefault && this.cards.length > 1) {
      Swal.fire({
        icon: 'warning',
        title: 'Tarjeta predeterminada',
        text: 'No puedes eliminar la tarjeta predeterminada. Primero establece otra tarjeta como predeterminada.',
        confirmButtonColor: '#3085d6'
      });
      return;
    }

    Swal.fire({
      title: '¿Eliminar tarjeta?',
      html: `
        <p>¿Estás seguro de eliminar la tarjeta terminada en <strong>${card.lastFourDigits}</strong>?</p>
        <p class="text-muted">Esta acción no se puede deshacer.</p>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar'
    }).then((result) => {
      if (result.isConfirmed) {
        const index = this.cards.findIndex(c => c.id === card.id);
        if (index > -1) {
          this.cards.splice(index, 1);

          Swal.fire({
            icon: 'success',
            title: '¡Eliminada!',
            text: 'La tarjeta ha sido eliminada',
            confirmButtonColor: '#3085d6',
            timer: 2000
          });
        }
      }
    });
  }

  getCardIcon(cardType: string): string {
    const icons: { [key: string]: string } = {
      'visa': 'visa.svg',
      'mastercard': 'mastercard.svg',
      'amex': 'american-express.svg'
    };
    return icons[cardType] || 'bi-credit-card';
  }

  getCardColor(cardType: string): string {
    const colors: { [key: string]: string } = {
      'visa': 'primary',
      'mastercard': 'warning',
      'amex': 'info',
      'discover': 'success'
    };
    return colors[cardType] || 'primary';
  }

  formatExpiryDisplay(month: string, year: string): string {
    return `${month}/${year.slice(-2)}`;
  }

  getYearOptions(): string[] {
    const currentYear = new Date().getFullYear();
    const years: string[] = [];
    for (let i = 0; i < 15; i++) {
      years.push((currentYear + i).toString());
    }
    return years;
  }

  getMonthOptions(): string[] {
    return Array.from({ length: 12 }, (_, i) => (i + 1).toString().padStart(2, '0'));
  }

  isCardNumberInvalid(): boolean {
    return this.submitted && this.cardNumber.replace(/\D/g, '').length !== 16;
  }

  getCleanCardNumber(): string {
    return this.cardNumber.replace(/\D/g, '');
  }
}
