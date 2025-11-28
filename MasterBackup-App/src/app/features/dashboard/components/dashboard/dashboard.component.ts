import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  userName: string = 'Usuario';
  
  constructor() {}

  ngOnInit(): void {
    // Obtener datos del usuario del localStorage o servicio
    const userData = localStorage.getItem('user');
    if (userData) {
      try {
        const user = JSON.parse(userData);
        this.userName = `${user.firstName || 'Usuario'} ${user.lastName || ''}`.trim();
      } catch (e) {
        console.error('Error parsing user data:', e);
      }
    }
  }
}
