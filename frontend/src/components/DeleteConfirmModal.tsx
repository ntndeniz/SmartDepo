import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';

interface Props {
  open: boolean;
  title?: string;
  message?: string;
  loading?: boolean;
  onConfirm: () => void;
  onClose: () => void;
}

export default function DeleteConfirmModal({
  open,
  title = 'Silme Onayı',
  message = 'Bu kaydı silmek istediğinize emin misiniz? (Kayıt soft-delete ile arşivlenir)',
  loading = false,
  onConfirm,
  onClose,
}: Props) {
  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText>{message}</DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Vazgeç
        </Button>
        <Button onClick={onConfirm} color="error" variant="contained" disabled={loading}>
          Sil
        </Button>
      </DialogActions>
    </Dialog>
  );
}
