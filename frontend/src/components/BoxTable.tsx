import { useCallback, useEffect, useState } from 'react';
import { Alert, Chip, IconButton, MenuItem, Snackbar, TextField } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import type { Box } from '../types';
import { getErrorMessage } from '../api/client';
import { boxApi } from '../api/services';
import PagedTable, { type Column } from './PagedTable';
import BoxFormModal from './BoxFormModal';
import DeleteConfirmModal from './DeleteConfirmModal';
import SummaryCards from './SummaryCards';
import PrintBarcodeButton from './PrintBarcodeButton';
import { formatDate } from '../utils/formatDate';

interface Props {
  companyId: string;
  onChanged?: () => void;
}

const statusColor = (status: string): 'success' | 'info' | 'default' => {
  if (status === 'InStock') return 'success';
  if (status === 'OnShelf') return 'info';
  return 'default';
};

const statusLabel = (status: string) =>
  status === 'InStock' ? 'Stokta' : status === 'OnShelf' ? 'Rafta' : 'Sevk Edildi';

export default function BoxTable({ companyId, onChanged }: Props) {
  const [rows, setRows] = useState<Box[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  // Varsayılan "Aktif" (Stokta+Rafta): sevk edilmiş/stoğu tükenmiş koliler yalnızca kullanıcı açıkça
  // seçtiğinde görünür — aksi halde liste zamanla sonsuza kadar büyüyen, çoğu artık ilgisiz kayıtlarla
  // dolu bir görünüme dönüşüyordu.
  const [status, setStatus] = useState('Active');
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Box | null>(null);
  const [deleting, setDeleting] = useState<Box | null>(null);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await boxApi.list({
        companyId,
        page,
        pageSize,
        search,
        status: status || undefined,
      });
      setRows(res.data);
      setTotalCount(res.totalCount);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
  }, [companyId, page, pageSize, search, status]);

  useEffect(() => {
    const timer = setTimeout(load, search ? 400 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  const handleSave = async (values: { productId: number; quantity: number; desi: number | null; reason?: string }) => {
    setSaving(true);
    try {
      if (editing) {
        const res = await boxApi.update({
          id: editing.id,
          companyId,
          quantity: values.quantity,
          desi: values.desi,
          reason: values.reason,
        });
        setMessage({ text: res.data.message || 'Koli güncellendi.', error: false });
      } else {
        const createdBy = sessionStorage.getItem('username') || '';
        await boxApi.create({ companyId, createdBy, ...values });
        setMessage({ text: 'Koli oluşturuldu.', error: false });
      }
      setFormOpen(false);
      setEditing(null);
      await load();
      onChanged?.();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleting) return;
    setSaving(true);
    try {
      await boxApi.delete({ id: deleting.id, companyId });
      setMessage({ text: 'Koli silindi.', error: false });
      setDeleting(null);
      await load();
      onChanged?.();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const columns: Column<Box>[] = [
    { key: 'barcode', label: 'Koli Barkodu' },
    { key: 'productBarcode', label: 'Ürün Barkodu' },
    { key: 'productName', label: 'Ürün' },
    { key: 'productColor', label: 'Renk' },
    { key: 'quantity', label: 'Miktar' },
    {
      key: 'desi',
      label: 'Desi',
      render: (row) => (row.desi != null ? row.desi : '-'),
    },
    {
      key: 'status',
      label: 'Durum',
      render: (row) => (
        <Chip size="small" color={statusColor(row.status)} label={statusLabel(row.status)} />
      ),
    },
    { key: 'createdBy', label: 'Oluşturan' },
    {
      key: 'createdAt',
      label: 'Oluşturma Tarihi',
      render: (row) => formatDate(row.createdAt),
    },
  ];

  return (
    <>
      <SummaryCards
        cards={[
          { label: 'Toplam Koli', value: totalCount },
          {
            label: 'Stoktaki (bu sayfa)',
            value: rows.filter((r) => r.status === 'InStock').length,
            color: '#2e7d32',
          },
          {
            label: 'Rafta (bu sayfa)',
            value: rows.filter((r) => r.status === 'OnShelf').length,
            color: '#0288d1',
          },
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
        onAdd={() => {
          setEditing(null);
          setFormOpen(true);
        }}
        addLabel="Yeni Koli"
        toolbar={
          <TextField
            select
            size="small"
            label="Durum"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
            sx={{ minWidth: 150 }}
          >
            <MenuItem value="Active">Aktif (Stokta + Rafta)</MenuItem>
            <MenuItem value="">Tümü</MenuItem>
            <MenuItem value="InStock">Stokta</MenuItem>
            <MenuItem value="OnShelf">Rafta</MenuItem>
            <MenuItem value="Dispatched">Sevk Edildi</MenuItem>
          </TextField>
        }
        renderActions={(row) => (
          <>
            <PrintBarcodeButton value={row.barcode} label={row.productName} />
            <IconButton
              size="small"
              onClick={() => {
                setEditing(row);
                setFormOpen(true);
              }}
            >
              <EditIcon fontSize="small" />
            </IconButton>
            <IconButton size="small" color="error" onClick={() => setDeleting(row)}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          </>
        )}
      />
      <BoxFormModal
        open={formOpen}
        companyId={companyId}
        box={editing}
        loading={saving}
        onSave={handleSave}
        onClose={() => {
          setFormOpen(false);
          setEditing(null);
        }}
      />
      <DeleteConfirmModal
        open={deleting !== null}
        loading={saving}
        onConfirm={handleDelete}
        onClose={() => setDeleting(null)}
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
