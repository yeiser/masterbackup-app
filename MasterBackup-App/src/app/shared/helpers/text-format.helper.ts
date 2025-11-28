import { FormGroup } from '@angular/forms';

/**
 * Clase utilitaria para manejar formateo de texto en formularios
 */
export class TextFormatHelper {
  
  /**
   * Capitaliza la primera letra de cada palabra
   * @param text Texto a capitalizar
   * @returns Texto con cada palabra capitalizada
   */
  static capitalizeText(text: string): string {
    if (!text) return '';
    
    return text
      .toLowerCase()
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  /**
   * Convierte texto a minúsculas
   * @param text Texto a convertir
   * @returns Texto en minúsculas
   */
  static toLowerCase(text: string): string {
    return text ? text.toLowerCase() : '';
  }

  /**
   * Convierte texto a mayúsculas
   * @param text Texto a convertir
   * @returns Texto en mayúsculas
   */
  static toUpperCase(text: string): string {
    return text ? text.toUpperCase() : '';
  }

  /**
   * Configura capitalización automática para uno o múltiples campos de un formulario
   * @param form FormGroup del formulario
   * @param fieldNames Nombre(s) de los campos a capitalizar
   * @returns Función para desuscribirse de los observables
   */
  static setupAutoCapitalize(form: FormGroup, ...fieldNames: string[]): () => void {
    const subscriptions: any[] = [];

    fieldNames.forEach(fieldName => {
      const subscription = form.get(fieldName)?.valueChanges.subscribe(value => {
        if (value && typeof value === 'string') {
          const capitalized = TextFormatHelper.capitalizeText(value);
          if (value !== capitalized) {
            form.get(fieldName)?.setValue(capitalized, { emitEvent: false });
          }
        }
      });

      if (subscription) {
        subscriptions.push(subscription);
      }
    });

    // Retornar función para limpiar las suscripciones
    return () => {
      subscriptions.forEach(sub => sub.unsubscribe());
    };
  }

  /**
   * Configura conversión automática a minúsculas para uno o múltiples campos
   * @param form FormGroup del formulario
   * @param fieldNames Nombre(s) de los campos a convertir
   * @returns Función para desuscribirse de los observables
   */
  static setupAutoLowerCase(form: FormGroup, ...fieldNames: string[]): () => void {
    const subscriptions: any[] = [];

    fieldNames.forEach(fieldName => {
      const subscription = form.get(fieldName)?.valueChanges.subscribe(value => {
        if (value && typeof value === 'string') {
          const lowerCased = value.toLowerCase();
          if (value !== lowerCased) {
            form.get(fieldName)?.setValue(lowerCased, { emitEvent: false });
          }
        }
      });

      if (subscription) {
        subscriptions.push(subscription);
      }
    });

    return () => {
      subscriptions.forEach(sub => sub.unsubscribe());
    };
  }

  /**
   * Configura conversión automática a mayúsculas para uno o múltiples campos
   * @param form FormGroup del formulario
   * @param fieldNames Nombre(s) de los campos a convertir
   * @returns Función para desuscribirse de los observables
   */
  static setupAutoUpperCase(form: FormGroup, ...fieldNames: string[]): () => void {
    const subscriptions: any[] = [];

    fieldNames.forEach(fieldName => {
      const subscription = form.get(fieldName)?.valueChanges.subscribe(value => {
        if (value && typeof value === 'string') {
          const upperCased = value.toUpperCase();
          if (value !== upperCased) {
            form.get(fieldName)?.setValue(upperCased, { emitEvent: false });
          }
        }
      });

      if (subscription) {
        subscriptions.push(subscription);
      }
    });

    return () => {
      subscriptions.forEach(sub => sub.unsubscribe());
    };
  }

  /**
   * Convierte campos específicos de un objeto a minúsculas
   * @param data Objeto con los datos
   * @param fieldNames Nombres de los campos a convertir
   * @returns Nuevo objeto con los campos especificados en minúsculas
   */
  static convertFieldsToLowerCase<T extends Record<string, any>>(
    data: T, 
    ...fieldNames: (keyof T)[]
  ): T {
    const result = { ...data };
    
    fieldNames.forEach(fieldName => {
      const value = result[fieldName];
      if (typeof value === 'string') {
        result[fieldName] = value.toLowerCase() as any;
      }
    });

    return result;
  }

  /**
   * Convierte campos específicos de un objeto a mayúsculas
   * @param data Objeto con los datos
   * @param fieldNames Nombres de los campos a convertir
   * @returns Nuevo objeto con los campos especificados en mayúsculas
   */
  static convertFieldsToUpperCase<T extends Record<string, any>>(
    data: T, 
    ...fieldNames: (keyof T)[]
  ): T {
    const result = { ...data };
    
    fieldNames.forEach(fieldName => {
      const value = result[fieldName];
      if (typeof value === 'string') {
        result[fieldName] = value.toUpperCase() as any;
      }
    });

    return result;
  }
}
