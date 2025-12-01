import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'relativeTime',
  standalone: true
})
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (!value) return 'Nunca';

    const date = typeof value === 'string' ? new Date(value) : value;
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 0) return 'Justo ahora';

    const intervals: { [key: string]: number } = {
      'año': 31536000,
      'mes': 2592000,
      'día': 86400,
      'hora': 3600,
      'minuto': 60,
      'segundo': 1
    };

    for (const [name, secondsInInterval] of Object.entries(intervals)) {
      const interval = Math.floor(seconds / secondsInInterval);
      
      if (interval >= 1) {
        if (interval === 1) {
          return `Hace 1 ${name}`;
        }
        
        // Pluralizar
        let pluralName = name;
        if (name === 'mes') {
          pluralName = 'meses';
        } else if (name === 'año') {
          pluralName = 'años';
        } else {
          pluralName = name + 's';
        }
        
        return `Hace ${interval} ${pluralName}`;
      }
    }

    return 'Justo ahora';
  }
}
