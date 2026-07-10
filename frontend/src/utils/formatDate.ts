export function formatDate(value: string | null | undefined): string {
  if (!value) return '-';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';
  // .NET DateTime.MinValue ("0001-01-01T00:00:00") — migration öncesi backfill edilmemiş eski
  // kayıtlarda görülür; "01.01.1 00:00" gibi anlamsız bir tarih yerine "—" gösterilir.
  if (date.getFullYear() <= 1) return '—';

  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');

  return `${day}.${month}.${year} ${hours}:${minutes}`;
}
