/// Bugünün tarihini yerel saat dilimine göre YYYY-MM-DD formatında döner — `<input type="date">`
/// değeri ve backend'in FromDate/ToDate parametreleri için kullanılır. `toISOString()` UTC'ye çevirdiği
/// için gece yarısına yakın saatlerde yanlış günü döndürebilir, bu yüzden yerel bileşenlerden elle
/// oluşturuluyor.
export function todayDateString(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
