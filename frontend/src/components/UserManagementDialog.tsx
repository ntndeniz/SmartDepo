import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import type { User } from '../types';
import { getErrorMessage } from '../api/client';
import { userApi } from '../api/services';
import { formatDate } from '../utils/formatDate';
import DeleteConfirmModal from './DeleteConfirmModal';

interface Props {
  open: boolean;
  companyId: string;
  onClose: () => void;
}

export default function UserManagementDialog({ open, companyId, onClose }: Props) {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [newUsername, setNewUsername] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [newRole, setNewRole] = useState<'Admin' | 'Staff'>('Staff');
  const [deleting, setDeleting] = useState<User | null>(null);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await userApi.list({ companyId, page: 1, pageSize: 100 });
      setUsers(res.data);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
  }, [companyId]);

  useEffect(() => {
    if (open) load();
  }, [open, load]);

  const handleCreate = async () => {
    setSaving(true);
    try {
      await userApi.create({ companyId, username: newUsername.trim(), password: newPassword, role: newRole });
      setMessage({ text: 'Kullanıcı oluşturuldu.', error: false });
      await load();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setNewUsername('');
      setNewPassword('');
      setNewRole('Staff');
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleting) return;
    setSaving(true);
    try {
      await userApi.delete({ id: deleting.id, companyId });
      setMessage({ text: 'Kullanıcı silindi.', error: false });
      setDeleting(null);
      await load();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
        <DialogTitle>Kullanıcılar</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography variant="subtitle2">Yeni Kullanıcı</Typography>
            <Stack direction="row" spacing={1}>
              <TextField
                size="small"
                label="Kullanıcı Adı"
                value={newUsername}
                onChange={(e) => setNewUsername(e.target.value)}
                sx={{ flex: 1 }}
              />
              <TextField
                size="small"
                type="password"
                label="Şifre"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                sx={{ flex: 1 }}
              />
              <TextField
                size="small"
                select
                label="Rol"
                value={newRole}
                onChange={(e) => setNewRole(e.target.value as 'Admin' | 'Staff')}
                sx={{ minWidth: 110 }}
              >
                <MenuItem value="Staff">Personel</MenuItem>
                <MenuItem value="Admin">Yönetici</MenuItem>
              </TextField>
              <Button
                variant="contained"
                disabled={saving || !newUsername.trim() || newPassword.length < 4}
                onClick={handleCreate}
              >
                Ekle
              </Button>
            </Stack>

            <Typography variant="subtitle2">Mevcut Kullanıcılar {loading ? '...' : `(${users.length})`}</Typography>
            {users.map((u) => (
              <Stack
                key={u.id}
                direction="row"
                spacing={2}
                sx={{ alignItems: 'center', justifyContent: 'space-between', border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 1 }}
              >
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <Typography variant="body2">
                    {u.username} <Typography component="span" variant="caption" color="text.secondary">— {formatDate(u.createdAt)}</Typography>
                  </Typography>
                  <Chip size="small" label={u.role === 'Admin' ? 'Yönetici' : 'Personel'} color={u.role === 'Admin' ? 'primary' : 'default'} />
                </Stack>
                <IconButton size="small" color="error" onClick={() => setDeleting(u)}>
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Kapat</Button>
        </DialogActions>
      </Dialog>

      <DeleteConfirmModal
        open={deleting !== null}
        loading={saving}
        onConfirm={handleDelete}
        onClose={() => setDeleting(null)}
        title="Kullanıcı Sil"
        message={`Kullanıcı "${deleting?.username}" silinecektir. Emin misiniz?`}
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
