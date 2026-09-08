import { Pipe, PipeTransform } from '@angular/core';

/**
 * Biçimlendiriciler bir kez kuruluyor: her hücre için yeni bir Intl.NumberFormat
 * yaratmak uzun tablolarda ölçülebilir bir maliyet.
 */
const money = new Intl.NumberFormat('tr-TR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const quantity = new Intl.NumberFormat('tr-TR', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 3,
});

const dateFormat = new Intl.DateTimeFormat('tr-TR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
});

const dateTimeFormat = new Intl.DateTimeFormat('tr-TR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

export const formatMoney = (value: number | null | undefined): string =>
  money.format(Number(value ?? 0));

export const formatQuantity = (value: number | null | undefined): string =>
  quantity.format(Number(value ?? 0));

/**
 * Sunucu tarihleri `DateOnly` olarak, yani saat dilimi taşımayan `yyyy-MM-dd`
 * metniyle gönderiyor. `new Date('2026-01-05')` bunu UTC gece yarısı sayıp negatif
 * dilimlerde bir gün geriye kaydırıyordu; gün/ay/yıl elle ayrıştırılıyor.
 */
export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return '';

  if (typeof value === 'string') {
    const dateOnly = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);

    if (dateOnly) {
      return `${dateOnly[3]}.${dateOnly[2]}.${dateOnly[1]}`;
    }
  }

  const parsed = value instanceof Date ? value : new Date(value);

  return Number.isNaN(parsed.getTime()) ? '' : dateFormat.format(parsed);
}

export function formatDateTime(value: string | Date | null | undefined): string {
  if (!value) return '';

  const parsed = value instanceof Date ? value : new Date(value);

  return Number.isNaN(parsed.getTime()) ? '' : dateTimeFormat.format(parsed);
}

/** `<input type="date">` alanlarının beklediği biçim. */
export function toInputDate(value: Date = new Date()): string {
  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');

  return `${value.getFullYear()}-${month}-${day}`;
}

/** Saniyeyi demo şeridindeki geri sayıma çevirir. */
export function formatCountdown(totalSeconds: number): string {
  const safe = Math.max(0, Math.floor(totalSeconds));
  const minutes = `${Math.floor(safe / 60)}`.padStart(2, '0');
  const seconds = `${safe % 60}`.padStart(2, '0');

  return `${minutes}:${seconds}`;
}

@Pipe({ name: 'money', standalone: true })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return formatMoney(value);
  }
}

@Pipe({ name: 'qty', standalone: true })
export class QuantityPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return formatQuantity(value);
  }
}

@Pipe({ name: 'trDate', standalone: true })
export class TrDatePipe implements PipeTransform {
  transform(value: string | Date | null | undefined, withTime = false): string {
    return withTime ? formatDateTime(value) : formatDate(value);
  }
}

/**
 * Listelerin arama kutusu. Nesnenin seçilen alanlarını tek bir metinde birleştirip
 * arıyor, böylece her ekran için ayrı bir boru yazmak gerekmiyor.
 */
@Pipe({ name: 'search', standalone: true })
export class SearchPipe implements PipeTransform {
  transform<T>(items: T[] | null | undefined, term: string, fields: string[]): T[] {
    if (!items?.length) return [];

    const needle = term?.trim().toLocaleLowerCase('tr');
    if (!needle) return items;

    return items.filter((item) =>
      fields.some((field) =>
        readPath(item, field).toLocaleLowerCase('tr').includes(needle)
      )
    );
  }
}

/** `customer.name` gibi noktalı yolları güvenle okur. */
function readPath(source: unknown, path: string): string {
  const value = path
    .split('.')
    .reduce<unknown>(
      (current, key) =>
        current && typeof current === 'object'
          ? (current as Record<string, unknown>)[key]
          : undefined,
      source
    );

  return value == null ? '' : String(value);
}
