import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { StorageService } from '../../../core/services/storage.service';
import { filter } from 'rxjs/operators';

interface PageInfo {
  title: string;
  icon: string;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit {
  userName: string = 'Usuario';
  userRole: string = 'Admin';
  currentPageTitle: string = 'Dashboard';
  currentPageIcon: string = 'fa-tachometer-alt';

  private pageRoutes: { [key: string]: PageInfo } = {
    '/dashboard': { 
      title: 'Dashboard', 
      icon: 'fa-tachometer-alt' 
    }
  };

  constructor(
    private router: Router,
    private storageService: StorageService
  ) {}

  ngOnInit(): void {
    const user = this.storageService.getCurrentUser();
    if (user) {
      this.userName = `${user.firstName || ''} ${user.lastName || ''}`.trim() || 'Usuario';
      this.userRole = user.role || 'Admin';
    }

    // Actualizar información de la página cuando cambie la ruta
    this.updatePageInfo(this.router.url);
    
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.updatePageInfo(event.urlAfterRedirects);
      });
  }

  private updatePageInfo(url: string): void {
    const pageInfo = this.pageRoutes[url] || { title: 'Dashboard', icon: 'fa-tachometer-alt' };
    this.currentPageTitle = pageInfo.title;
    this.currentPageIcon = pageInfo.icon;
  }

  logout(): void {
    // Limpiar todos los datos de sesión usando el servicio
    this.storageService.clearSession();
    this.router.navigate(['/login']);
  }
}
