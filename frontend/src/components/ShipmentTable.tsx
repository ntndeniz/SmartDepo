import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Paper,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import PlaylistAddIcon from '@mui/icons-material/PlaylistAdd';
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner';
import DoneAllIcon from '@mui/icons-material/DoneAll';
import FactCheckIcon from '@mui/icons-material/FactCheck';
import type { DispatchOrder, DispatchPallet } from '../types';
import { getErrorMessage } from '../api/client';
import { dispatchApi } from '../api/services';
import PagedTable, { type Column } from './PagedTable';
import BarcodeScannerModal from './BarcodeScannerModal';
import PrintBarcodeButton from './PrintBarcodeButton';
import PalletsSection from './PalletsSection';
import { formatDate } from '../utils/formatDate';
import { todayDateString } from '../utils/today';
import SummaryCards from './SummaryCards';

interface Props {
  companyId: string;
}

// DispatchOrder.Status yalnızca "toplama işi bitti mi" sorusuna cevap verir — palete konup konmadığı
// veya fiilen sevk edilip edilmediği ayrı bir kavramdır (bkz. DispatchPallet.Status, Paletler bölümü).
// Bu yüzden "Completed" burada YANLIŞ şekilde "Sevk Edildi" değil, "Tamamlandı" (toplama bitti,
// paletlemeyi bekliyor) olarak gösterilmelidir.
const dispatchStatusInfo = (status: string | undefined): { label: string; color: 'warning' | 'info' | 'success' | 'default' } => {
  if (status === 'Picking') return { label: 'Toplanıyor', color: 'warning' };
  if (status === 'PartiallyCompleted') return { label: 'Kısmi Tamamlandı', color: 'info' };
  if (status === 'Completed') return { label: 'Tamamlandı', color: 'success' };
  return { label: status ?? '-', color: 'default' };
};

// Sevkiyat: ürünler koridordan toplanır (pick list), koliler kapatılır, koliler paletlenir ve
// palet oluşturulunca ilgili sipariş "Siparişler" ekranında otomatik güncel durumla görünür.
export default function ShipmentTable({ companyId }: Props) {
  const [rows, setRows] = useState<DispatchOrder[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  // Durum filtresi kaldırıldı — bu tablo artık her zaman yalnızca AKTİF toplama işini (Picking)
  // gösterir, kullanıcı tarafından değiştirilemez. Tamamlanmış bir emrin durumu "Siparişler"
  // sekmesinde, kolisi ise aşağıdaki "Paletlenmemiş Koliler" bölümünde barkoduyla zaten görünür.
  const ACTIVE_STATUS = 'Picking';
  // Varsayılan olarak yalnızca BUGÜNÜN aktif işleri gösterilir (günlük operasyon akışına uygun);
  // kullanıcı isterse tarih aralığını genişletip önceki günlerden kalan aktif işleri de görebilir.
  const [fromDate, setFromDate] = useState(todayDateString());
  const [toDate, setToDate] = useState(todayDateString());
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  // Sipariş barkodu okutarak dağıtım emri (toplama) başlatma
  const [orderCodeInput, setOrderCodeInput] = useState('');
  const [orderCodeScannerOpen, setOrderCodeScannerOpen] = useState(false);

  // Palet kontrol
  const [palletCheckOpen, setPalletCheckOpen] = useState(false);
  const [palletCheckBarcode, setPalletCheckBarcode] = useState('');
  const [palletCheckResult, setPalletCheckResult] = useState<DispatchPallet | null>(null);
  const [palletCheckError, setPalletCheckError] = useState<string | null>(null);

  // Detay diyaloğu
  const [detailOrder, setDetailOrder] = useState<DispatchOrder | null>(null);
  const [pickScanInput, setPickScanInput] = useState('');
  const [pickTally, setPickTally] = useState<Record<string, number>>({});
  const [pickScannerOpen, setPickScannerOpen] = useState(false);

  // Bir koli kapatıldığında Paletler bölümünün ("paletlenmemiş koliler") yeniden yüklenmesini tetikler.
  const [palletsRefreshKey, setPalletsRefreshKey] = useState(0);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await dispatchApi.list({
        companyId,
        page,
        pageSize,
        search,
        status: ACTIVE_STATUS,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      });
      setRows(res.data);
      setTotalCount(res.totalCount);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [companyId, page, pageSize, search, fromDate, toDate]);

  useEffect(() => {
    const timer = setTimeout(load, search ? 400 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  const createdBy = () => sessionStorage.getItem('username') || '';

  const handleCreateFromOrderCode = async (code: string) => {
    const trimmed = code.trim();
    if (!trimmed) return;
    setSaving(true);
    try {
      const res = await dispatchApi.createFromStoreOrder({
        companyId,
        storeOrderCode: trimmed,
        createdBy: createdBy(),
      });
      if (res.data.success) {
        setMessage({ text: 'Dağıtım emri oluşturuldu, toplama listesi hazır.', error: false });
        setOrderCodeInput('');
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const openDetail = (order: DispatchOrder) => {
    setDetailOrder(order);
    setPickScanInput('');
    setPickTally({});
  };

  const registerPickScan = (barcode: string) => {
    const trimmed = barcode.trim();
    if (!trimmed || !detailOrder) return;

    const item = detailOrder.items.find((i) => i.productBarcode === trimmed);
    if (!item) {
      setMessage({ text: 'Bu barkod bu siparişin toplama listesinde yok.', error: true });
      return;
    }

    const alreadyTallied = pickTally[trimmed] ?? 0;
    const remaining = item.requestedQuantity - item.pickedQuantity - alreadyTallied;
    if (remaining <= 0) {
      setMessage({ text: `${item.productName} için istenen miktar zaten toplandı.`, error: true });
      return;
    }

    setPickTally((prev) => ({ ...prev, [trimmed]: (prev[trimmed] ?? 0) + 1 }));
    setPickScanInput('');
  };

  const handleCloseBox = async () => {
    if (!detailOrder) return;
    const items = Object.entries(pickTally).map(([productBarcode, quantity]) => ({
      productBarcode,
      quantity,
    }));
    if (items.length === 0) return;

    setSaving(true);
    try {
      const res = await dispatchApi.closeBox({
        companyId,
        dispatchOrderId: detailOrder.id,
        createdBy: createdBy(),
        items,
      });
      if (res.data.success && res.data.data) {
        setMessage({ text: res.data.message || 'Sevkiyat kolisi oluşturuldu.', error: false });
        setDetailOrder(res.data.data);
        setPickTally({});
        setPalletsRefreshKey((k) => k + 1);
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleComplete = async (forcePartial = false) => {
    if (!detailOrder) return;
    setSaving(true);
    try {
      const res = await dispatchApi.complete({ companyId, id: detailOrder.id, forcePartial });
      if (res.data.success && res.data.data) {
        setMessage({ text: res.data.message || 'Dağıtım emri tamamlandı.', error: false });
        setDetailOrder(res.data.data);
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handlePalletCheck = async (barcode: string) => {
    const trimmed = barcode.trim();
    if (!trimmed) return;
    setPalletCheckError(null);
    setPalletCheckResult(null);
    try {
      const res = await dispatchApi.getPalletByBarcode(companyId, trimmed);
      if (res.data.success && res.data.data) {
        setPalletCheckResult(res.data.data);
      } else {
        setPalletCheckError(res.data.message || 'Palet bulunamadı');
      }
    } catch (err) {
      setPalletCheckError(getErrorMessage(err));
    }
  };

  const columns: Column<DispatchOrder>[] = [
    { key: 'storeName', label: 'Mağaza Adı' },
    { key: 'storeId', label: 'Mağaza ID' },
    { key: 'storeOrderCode', label: 'Sipariş Barkodu' },
    {
      key: 'status',
      label: 'Durum',
      render: (row) => {
        const info = dispatchStatusInfo(row.status);
        return <Chip size="small" color={info.color} label={info.label} />;
      },
    },
    {
      key: 'createdAt',
      label: 'Oluşturulma',
      render: (row) => formatDate(row.createdAt),
    },
    {
      key: 'boxes',
      label: 'Sevkiyat Kolisi',
      render: (row) => row.boxes.length,
    },
    {
      key: 'boxBarcodes',
      label: 'Koli Barkodları',
      render: (row) =>
        row.boxes.length === 0 ? (
          <Typography variant="caption" color="text.secondary">
            —
          </Typography>
        ) : (
          <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5 }}>
            {row.boxes.map((b) => (
              <Chip key={b.id} size="small" label={b.barcode} />
            ))}
          </Stack>
        ),
    },
  ];

  return (
    <>
      <SummaryCards
        cards={[
          { label: 'Aktif Toplama İşi', value: totalCount, color: '#ed6c02' },
        ]}
      />

      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
        <TextField
          size="small"
          label="Sipariş Barkodu Okut"
          value={orderCodeInput}
          onChange={(e) => setOrderCodeInput(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') handleCreateFromOrderCode(orderCodeInput);
          }}
        />
        <IconButton onClick={() => setOrderCodeScannerOpen(true)} title="Kamera ile tara">
          <QrCodeScannerIcon />
        </IconButton>
        <Button
          variant="contained"
          disabled={!orderCodeInput.trim() || saving}
          onClick={() => handleCreateFromOrderCode(orderCodeInput)}
        >
          Toplamaya Başla
        </Button>
        <Button
          variant="outlined"
          startIcon={<FactCheckIcon />}
          onClick={() => {
            setPalletCheckOpen(true);
            setPalletCheckBarcode('');
            setPalletCheckResult(null);
            setPalletCheckError(null);
          }}
        >
          Palet Kontrol
        </Button>
      </Stack>

      <PagedTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        totalCount={totalCount}
        page={page}
        pageSize={pageSize}
        loading={loading}
        search={search}
        onSearchChange={(value) => {
          setSearch(value);
          setPage(1);
        }}
        onPageChange={setPage}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setPage(1);
        }}
        toolbar={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <TextField
              size="small"
              type="date"
              label="Başlangıç"
              value={fromDate}
              onChange={(e) => {
                setFromDate(e.target.value);
                setPage(1);
              }}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              size="small"
              type="date"
              label="Bitiş"
              value={toDate}
              onChange={(e) => {
                setToDate(e.target.value);
                setPage(1);
              }}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            {(fromDate !== todayDateString() || toDate !== todayDateString()) && (
              <Button
                size="small"
                onClick={() => {
                  setFromDate(todayDateString());
                  setToDate(todayDateString());
                  setPage(1);
                }}
              >
                Bugüne Dön
              </Button>
            )}
          </Stack>
        }
        renderActions={(row) => (
          <IconButton size="small" title="Toplama listesi ve koliler" onClick={() => openDetail(row)}>
            <PlaylistAddIcon fontSize="small" />
          </IconButton>
        )}
      />

      {/* Dağıtım emri detayı: toplama listesi + koliler + palet oluşturma */}
      <Dialog open={detailOrder !== null} onClose={() => setDetailOrder(null)} maxWidth="md" fullWidth>
        <DialogTitle>
          Dağıtım Emri #{detailOrder?.id} — {detailOrder?.storeName}{' '}
          <Chip
            size="small"
            sx={{ ml: 1 }}
            color={dispatchStatusInfo(detailOrder?.status).color}
            label={dispatchStatusInfo(detailOrder?.status).label}
          />
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography variant="caption" color="text.secondary">
              Mağaza ID: {detailOrder?.storeId} | Adres: {detailOrder?.address}
            </Typography>

            <Typography variant="subtitle2">Toplama Listesi (Pick List)</Typography>
            {detailOrder?.items.map((item) => {
              const tallied = pickTally[item.productBarcode] ?? 0;
              const effectivePicked = item.pickedQuantity + tallied;
              const stillNeeded = effectivePicked < item.requestedQuantity;
              return (
                <Paper key={item.id} sx={{ p: 1.5 }}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
                    <Typography variant="body2">
                      {item.productBarcode} | {item.productName} | Renk: {item.color}
                    </Typography>
                    <Chip
                      size="small"
                      color={effectivePicked >= item.requestedQuantity ? 'success' : 'default'}
                      label={`${effectivePicked} / ${item.requestedQuantity}`}
                    />
                  </Stack>
                  {stillNeeded && (
                    <Box sx={{ mt: 1, pl: 1, borderLeft: '2px solid', borderColor: 'divider' }}>
                      {item.suggestions.length === 0 ? (
                        <Typography variant="caption" color="error">
                          Bu ürün depoda hiç yok (stok bulunamadı).
                        </Typography>
                      ) : item.suggestions[0].locationBarcode ? (
                        <Stack spacing={0.5}>
                          {item.suggestions.map((s, index) => (
                            <Typography key={s.boxBarcode} variant="caption" sx={{ display: 'block' }}>
                              {index === 0 ? '👉 Önce buradan alın: ' : `${index + 1}. yetmezse devam edin: `}
                              <strong>Raf {s.locationBarcode}</strong> — koli {s.boxBarcode} ({s.availableQuantity} adet)
                            </Typography>
                          ))}
                        </Stack>
                      ) : (
                        <Stack spacing={0.5}>
                          <Typography variant="caption" color="warning.main">
                            Bu ürün henüz hiçbir rafa yerleştirilmemiş — aşağıdaki koli(ler)i depoda
                            (ör. mal kabul alanında) arayın:
                          </Typography>
                          {item.suggestions.map((s) => (
                            <Typography key={s.boxBarcode} variant="caption" sx={{ display: 'block' }}>
                              📦 koli <strong>{s.boxBarcode}</strong> ({s.availableQuantity} adet)
                            </Typography>
                          ))}
                        </Stack>
                      )}
                    </Box>
                  )}
                </Paper>
              );
            })}

            {detailOrder?.status === 'Picking' && (
              <>
                <Divider>Ürün Barkodu Okutarak Topla</Divider>
                <Stack direction="row" spacing={1}>
                  <TextField
                    size="small"
                    label="Ürün Barkodu Okut"
                    value={pickScanInput}
                    onChange={(e) => setPickScanInput(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') registerPickScan(pickScanInput);
                    }}
                    sx={{ flex: 1 }}
                    autoFocus
                  />
                  <IconButton onClick={() => setPickScannerOpen(true)} title="Kamera ile tara">
                    <QrCodeScannerIcon />
                  </IconButton>
                </Stack>
                {Object.keys(pickTally).length > 0 && (
                  <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                    {Object.entries(pickTally).map(([barcode, count]) => (
                      <Chip key={barcode} label={`${barcode}: ${count}`} sx={{ mb: 0.5 }} />
                    ))}
                  </Stack>
                )}
                <Button
                  variant="contained"
                  disabled={Object.keys(pickTally).length === 0 || saving}
                  onClick={handleCloseBox}
                >
                  Koli Kapat (Koli Barkodu Otomatik Oluşturulacak)
                </Button>
              </>
            )}

            <Divider>Sevkiyat Kolileri</Divider>
            {detailOrder?.boxes.length === 0 ? (
              <Typography color="text.secondary" variant="body2">
                Henüz kapatılmış koli yok.
              </Typography>
            ) : (
              detailOrder?.boxes.map((box) => (
                <Paper key={box.id} sx={{ p: 1.5 }}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
                    <Typography variant="subtitle2">📦 {box.barcode}</Typography>
                    <PrintBarcodeButton value={box.barcode} label={detailOrder.storeName} />
                  </Stack>
                  <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', mt: 0.5 }}>
                    {box.items.map((item) => (
                      <Chip
                        key={item.id}
                        size="small"
                        label={
                          item.pickedFromLocationBarcode
                            ? `${item.productName} × ${item.quantity} (${item.pickedFromLocationBarcode})`
                            : `${item.productName} × ${item.quantity}`
                        }
                      />
                    ))}
                  </Stack>
                </Paper>
              ))
            )}

            {detailOrder && detailOrder.boxes.length > 0 && (
              <Alert severity="info">
                Bu koliler "Sevkiyat → Paletler" bölümünde paletlenmeyi bekliyor. Bir palete yalnızca
                aynı mağazanın kolileri eklenebilir.
              </Alert>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailOrder(null)}>Kapat</Button>
          {detailOrder?.status === 'Picking' && (
            <>
              <Button
                variant="contained"
                color="success"
                startIcon={<DoneAllIcon />}
                onClick={() => handleComplete(false)}
                disabled={
                  saving ||
                  detailOrder.items.some((i) => i.pickedQuantity < i.requestedQuantity)
                }
              >
                Emri Tamamla
              </Button>
              {detailOrder.items.some((i) => i.pickedQuantity < i.requestedQuantity) &&
                detailOrder.items.some((i) => i.pickedQuantity > 0) && (
                  <Button
                    variant="outlined"
                    color="warning"
                    onClick={() => handleComplete(true)}
                    disabled={saving}
                  >
                    Stok Yetersiz — Kısmi Tamamla
                  </Button>
                )}
            </>
          )}
        </DialogActions>
      </Dialog>

      {/* Palet Kontrol */}
      <Dialog open={palletCheckOpen} onClose={() => setPalletCheckOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Palet Kontrol</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Palet Barkodu"
              value={palletCheckBarcode}
              onChange={(e) => setPalletCheckBarcode(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') handlePalletCheck(palletCheckBarcode);
              }}
              fullWidth
              autoFocus
            />
            <Button variant="outlined" onClick={() => handlePalletCheck(palletCheckBarcode)}>
              Kontrol Et
            </Button>
            {palletCheckError && <Alert severity="error">{palletCheckError}</Alert>}
            {palletCheckResult && (
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle2">{palletCheckResult.barcode}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {palletCheckResult.storeName} ({palletCheckResult.storeId})
                </Typography>
                <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
                  <Chip
                    label={
                      palletCheckResult.status === 'Ready'
                        ? 'Sevke Hazır'
                        : palletCheckResult.status === 'Shipped'
                          ? 'Sevk Edildi'
                          : 'Hazırlanıyor'
                    }
                  />
                  <Chip label={`Koli Sayısı: ${palletCheckResult.boxCount}`} />
                  <Chip label={`Toplam Ürün: ${palletCheckResult.totalItemQuantity}`} />
                </Stack>
              </Paper>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPalletCheckOpen(false)}>Kapat</Button>
        </DialogActions>
      </Dialog>

      <BarcodeScannerModal
        open={orderCodeScannerOpen}
        onClose={() => setOrderCodeScannerOpen(false)}
        onScan={(barcode) => {
          setOrderCodeScannerOpen(false);
          setOrderCodeInput(barcode);
        }}
      />
      <BarcodeScannerModal
        open={pickScannerOpen}
        onClose={() => setPickScannerOpen(false)}
        onScan={(barcode) => {
          setPickScannerOpen(false);
          registerPickScan(barcode);
        }}
      />
      <PalletsSection companyId={companyId} refreshKey={palletsRefreshKey} />

      <Snackbar
        open={message !== null}
        autoHideDuration={4000}
        onClose={() => setMessage(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity={message?.error ? 'error' : 'success'} onClose={() => setMessage(null)}>
          {message?.text}
        </Alert>
      </Snackbar>
    </>
  );
}
