import { useCallback, useEffect, useState } from 'react';
import { Alert, IconButton, Snackbar } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import type { Store } from '../types';
import { getErrorMessage } from '../api/client';
import { storeApi } from '../api/services';
import PagedTable, { type Column } from './PagedTable';
import StoreFormModal from './StoreFormModal';
import DeleteConfirmModal from './DeleteConfirmModal';
import SummaryCards from './SummaryCards';
import { formatDate } from '../utils/formatDate';

interface Props {
  companyId: string;
  isAdmin: boolean;
}

export default function StoreTable({ companyId, isAdmin }: Props) {
  const [rows, setRows] = useState<Store[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Store | null>(null);
  const [deleting, setDeleting] = useState<Store | null>(null);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await storeApi.list({ companyId, page, pageSize, search });
      setRows(res.data);
      setTotalCount(res.totalCount);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
  }, [companyId, page, pageSize, search]);

  useEffect(() => {
    const timer = setTimeout(load, search ? 400 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  const handleSave = async (values: { name: string; address: string }) => {
    setSaving(true);
    try {
      if (editing) {
        await storeApi.update({ id: editing.id, companyId, ...values });
        setMessage({ text: 'Mağaza güncellendi.', error: false });
      } else {
        await storeApi.create({ companyId, ...values });
        setMessage({ text: 'Mağaza oluşturuldu.', error: false });
      }
      setFormOpen(false);
      setEditing(null);
      await load();
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
      await storeApi.delete({ id: deleting.id, companyId });
      setMessage({ text: 'Mağaza silindi.', error: false });
      setDeleting(null);
      await load();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const columns: Column<Store>[] = [
    { key: 'storeCode', label: 'Mağaza ID' },
    { key: 'name', label: 'Mağaza Adı' },
    { key: 'address', label: 'Adres' },
    { key: 'createdAt', label: 'Oluşturulma', render: (row) => formatDate(row.createdAt) },
  ];

  return (
    <>
      <SummaryCards cards={[{ label: 'Toplam Mağaza', value: totalCount }]} />
      <PagedTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        totalCount={totalCount}
        page={page}
        pageSize={pageSize}
        loading={loading}
        search={search}
        onSearchChange={(v) => {
          setSearch(v);
          setPage(1);
        }}
        onPageChange={setPage}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setPage(1);
        }}
        onAdd={
          isAdmin
            ? () => {
                setEditing(null);
                setFormOpen(true);
              }
            : undefined
        }
        addLabel="Yeni Mağaza"
        renderActions={(row) => (
          <>
            {isAdmin && (
              <IconButton
                size="small"
                onClick={() => {
                  setEditing(row);
                  setFormOpen(true);
                }}
              >
                <EditIcon fontSize="small" />
              </IconButton>
            )}
            {isAdmin && (
              <IconButton size="small" color="error" onClick={() => setDeleting(row)}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            )}
          </>
        )}
      />

      <StoreFormModal
        open={formOpen}
        store={editing}
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
        title="Mağaza Sil"
        message={`Mağaza "${deleting?.name}" silinecektir. Emin misiniz?`}
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
