import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Paper,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { StoreOrder } from '../types';
import { getErrorMessage } from '../api/client';
import { dispatchApi, storeOrderApi } from '../api/services';
import PagedTable, { type Column } from './PagedTable';
import StoreOrderFormModal from './StoreOrderFormModal';
import StoreOrderPdfUploadModal from './StoreOrderPdfUploadModal';
import SummaryCards from './SummaryCards';
import { formatDate } from '../utils/formatDate';
import { todayDateString } from '../utils/today';

interface Props {
  companyId: string;
}

const statusInfo = (status: string | null): { label: string; color: 'default' | 'warning' | 'info' | 'success' } => {
  if (status === null) return { label: 'Bekliyor', color: 'default' };
  if (status === 'Picking') return { label: 'Toplanıyor', color: 'warning' };
  if (status === 'PartiallyCompleted') return { label: 'Kısmi Tamamlandı', color: 'info' };
  if (status === 'ReadyToShip') return { label: 'Sevke Hazır', color: 'warning' };
  if (status === 'Shipped') return { label: 'Sevk Edildi', color: 'success' };
  return { label: 'Tamamlandı', color: 'info' };
};

export default function OrdersTable({ companyId }: Props) {
  const [rows, setRows] = useState<StoreOrder[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  // Varsayılan olarak yalnızca BUGÜN oluşturulan siparişler gösterilir (günlük operasyon akışına
  // uygun); kullanıcı isterse tarih aralığını genişletip geçmiş günlerin siparişlerini de görebilir.
  const [fromDate, setFromDate] = useState(todayDateString());
  const [toDate, setToDate] = useState(todayDateString());
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const [storeOrderFormOpen, setStoreOrderFormOpen] = useState(false);
  const [pdfUploadOpen, setPdfUploadOpen] = useState(false);
  const [detailOrder, setDetailOrder] = useState<StoreOrder | null>(null);

  const createdBy = () => sessionStorage.getItem('username') || '';

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await storeOrderApi.list({
        companyId,
        page,
        pageSize,
        search,
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
  }, [companyId, page, pageSize, search, fromDate, toDate]);

  useEffect(() => {
    const timer = setTimeout(load, search ? 400 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  const handleCreateStoreOrder = async (values: {
    storeName: string;
    address: string;
    items: { productId: number; color: string; quantity: number }[];
  }) => {
    setSaving(true);
    try {
      const res = await storeOrderApi.create({ companyId, ...values });
      if (res.data.success && res.data.data) {
        setMessage({ text: res.data.message || 'Mağaza siparişi oluşturuldu.', error: false });
        setStoreOrderFormOpen(false);
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

  const handleCreateStoreOrderFromPdf = async (values: {
    storeName: string;
    address: string;
    items: { productId: number; color: string; quantity: number }[];
  }) => {
    setSaving(true);
    try {
      const res = await storeOrderApi.create({ companyId, ...values });
      if (res.data.success && res.data.data) {
        setMessage({ text: res.data.message || "Mağaza siparişi PDF'ten oluşturuldu.", error: false });
        setPdfUploadOpen(false);
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

  const handleStartPicking = async (order: StoreOrder) => {
    setSaving(true);
    try {
      const res = await dispatchApi.createFromStoreOrder({
        companyId,
        storeOrderCode: order.orderCode,
        createdBy: createdBy(),
      });
      if (res.data.success) {
        setMessage({ text: 'Toplama başlatıldı, "Sevkiyat" sekmesinden devam edin.', error: false });
        setDetailOrder(null);
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

  const columns: Column<StoreOrder>[] = [
    { key: 'storeName', label: 'Mağaza Adı' },
    { key: 'storeId', label: 'Mağaza ID' },
    { key: 'orderCode', label: 'Sipariş Barkodu' },
    {
      key: 'dispatchStatus',
      label: 'Durum',
      render: (row) => {
        const info = statusInfo(row.dispatchStatus);
        return <Chip size="small" color={info.color} label={info.label} />;
      },
    },
    { key: 'items', label: 'Ürün Çeşidi', render: (row) => row.items.length },
    { key: 'createdAt', label: 'Oluşturulma', render: (row) => formatDate(row.createdAt) },
  ];

  const waiting = rows.filter((r) => r.dispatchStatus === null).length;

  return (
    <>
      <SummaryCards
        cards={[
          { label: 'Toplam Sipariş', value: totalCount },
          { label: 'Bekleyen (bu sayfa)', value: waiting, color: '#757575' },
        ]}
      />

      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
        <Button variant="contained" onClick={() => setStoreOrderFormOpen(true)}>
          Yeni Mağaza Siparişi
        </Button>
        <Button variant="outlined" onClick={() => setPdfUploadOpen(true)}>
          PDF'ten Yükle
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
          <Button size="small" onClick={() => setDetailOrder(row)}>
            Detay
          </Button>
        )}
      />

      <StoreOrderFormModal
        open={storeOrderFormOpen}
        companyId={companyId}
        loading={saving}
        onSave={handleCreateStoreOrder}
        onClose={() => setStoreOrderFormOpen(false)}
      />

      <StoreOrderPdfUploadModal
        open={pdfUploadOpen}
        loading={saving}
        onConfirm={handleCreateStoreOrderFromPdf}
        onClose={() => setPdfUploadOpen(false)}
      />

      <Dialog open={detailOrder !== null} onClose={() => setDetailOrder(null)} maxWidth="sm" fullWidth>
        <DialogTitle>
          Sipariş — {detailOrder?.storeName}{' '}
          {detailOrder && (
            <Chip size="small" sx={{ ml: 1 }} {...statusInfo(detailOrder.dispatchStatus)} />
          )}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography variant="caption" color="text.secondary">
              Mağaza ID: {detailOrder?.storeId} | Sipariş Barkodu: {detailOrder?.orderCode} | Adres:{' '}
              {detailOrder?.address}
            </Typography>
            <Typography variant="subtitle2">İstenen Ürünler</Typography>
            {detailOrder?.items.map((item) => (
              <Paper key={item.id} sx={{ p: 1.5 }}>
                <Typography variant="body2">
                  {item.productBarcode} | {item.productName} | Renk: {item.color} | Miktar:{' '}
                  {item.quantity}
                </Typography>
              </Paper>
            ))}
            {detailOrder?.dispatchStatus === null && (
              <Alert severity="info">
                Bu sipariş için henüz toplama başlatılmadı. "Toplamaya Başla" ile dağıtım emri açıp
                "Sevkiyat" sekmesinden devam edebilirsiniz.
              </Alert>
            )}
            {detailOrder?.dispatchStatus !== null && detailOrder?.dispatchStatus !== undefined && (
              <Alert severity="info">
                Toplama/sevkiyat işlemleri "Sevkiyat" sekmesinden yürütülür.
              </Alert>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailOrder(null)}>Kapat</Button>
          {detailOrder?.dispatchStatus === null && (
            <Button
              variant="contained"
              disabled={saving}
              onClick={() => detailOrder && handleStartPicking(detailOrder)}
            >
              Toplamaya Başla
            </Button>
          )}
        </DialogActions>
      </Dialog>

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
