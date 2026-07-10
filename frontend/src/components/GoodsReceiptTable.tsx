import { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Snackbar, Stack, TextField } from '@mui/material';
import type { GoodsReceiptItem } from '../types';
import { getErrorMessage } from '../api/client';
import { goodsReceiptApi } from '../api/services';
import PagedTable, { type Column } from './PagedTable';
import GoodsReceiptFormModal from './GoodsReceiptFormModal';
import SummaryCards from './SummaryCards';
import PrintBarcodeButton from './PrintBarcodeButton';
import { formatDate } from '../utils/formatDate';
import { todayDateString } from '../utils/today';

interface Props {
  companyId: string;
}

export default function GoodsReceiptTable({ companyId }: Props) {
  const [rows, setRows] = useState<GoodsReceiptItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  // Varsayılan olarak yalnızca BUGÜN yapılan mal kabuller gösterilir; kullanıcı isterse tarih
  // aralığını genişletip geçmiş günlerin kayıtlarını da görebilir.
  const [fromDate, setFromDate] = useState(todayDateString());
  const [toDate, setToDate] = useState(todayDateString());
  const [loading, setLoading] = useState(false);

  const [formOpen, setFormOpen] = useState(false);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await goodsReceiptApi.listItems({
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

  const totalCounted = rows.reduce((sum, r) => sum + r.countedQuantity, 0);

  const handleSave = async () => {
    setFormOpen(false);
    setMessage({ text: 'Mal kabul oturumu tamamlandı.', error: false });
    setPage(1);
    await load();
  };

  const columns: Column<GoodsReceiptItem>[] = [
    { key: 'productBarcode', label: 'Ürün Barkod' },
    { key: 'brandName', label: 'Marka İsmi' },
    { key: 'productName', label: 'Ürün İsmi' },
    { key: 'productColor', label: 'Renk' },
    { key: 'countedQuantity', label: 'Kalem (Miktar)' },
    {
      key: 'createdAt',
      label: 'İşlem Tarihi',
      render: (row) => formatDate(row.createdAt),
    },
  ];

  return (
    <>
      <SummaryCards
        cards={[
          { label: 'Toplam Kalem', value: totalCount },
          { label: 'Bu Sayfadaki Sayım Toplamı', value: totalCounted },
        ]}
      />
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
        onAdd={() => setFormOpen(true)}
        addLabel="Yeni Mal Kabul"
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
          <PrintBarcodeButton value={row.boxBarcode} label={row.productName} />
        )}
      />
      <GoodsReceiptFormModal
        open={formOpen}
        companyId={companyId}
        loading={false}
        onSave={handleSave}
        onClose={() => setFormOpen(false)}
      />
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
