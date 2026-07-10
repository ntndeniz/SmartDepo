import { useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import DownloadIcon from '@mui/icons-material/Download';
import DeleteIcon from '@mui/icons-material/Delete';
import { getErrorMessage } from '../api/client';
import { productApi } from '../api/services';
import { parseCsv } from '../utils/parseCsv';

const UNITS = ['Adet', 'Kg', 'Koli', 'Paket'];

interface DraftRow {
  name: string;
  color: string;
  unit: string;
}

interface Props {
  open: boolean;
  companyId: string;
  brandId: number | null;
  brandName: string;
  loading?: boolean;
  onUploaded: () => void;
  onClose: () => void;
}

const TEMPLATE_CSV =
  'Ürün Adı,Renk,Birim\nÖrnek Tişört,beyaz,Adet\nÖrnek Pantolon,siyah,Adet\n';

export default function ProductBulkUploadModal({
  open,
  companyId,
  brandId,
  brandName,
  loading,
  onUploaded,
  onClose,
}: Props) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [rows, setRows] = useState<DraftRow[]>([]);
  const [parseError, setParseError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [resultMessage, setResultMessage] = useState<{ text: string; error: boolean } | null>(null);
  const [rowErrors, setRowErrors] = useState<Record<number, string>>({});

  const reset = () => {
    setRows([]);
    setParseError(null);
    setResultMessage(null);
    setRowErrors({});
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleDownloadTemplate = () => {
    const blob = new Blob([TEMPLATE_CSV], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'urun-toplu-yukleme-sablonu.csv';
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  };

  const handleFileSelect = async (file: File) => {
    setParseError(null);
    setResultMessage(null);
    setRowErrors({});
    try {
      const text = await file.text();
      const parsedRows = parseCsv(text);
      if (parsedRows.length < 2) {
        setParseError('Dosyada başlık satırından sonra en az bir ürün satırı olmalı.');
        return;
      }
      // İlk satır başlık kabul edilir (Ürün Adı, Renk, Birim sırasıyla).
      const dataRows = parsedRows.slice(1);
      const draft: DraftRow[] = dataRows.map((cols) => ({
        name: (cols[0] ?? '').trim(),
        color: (cols[1] ?? '').trim(),
        unit: (cols[2] ?? '').trim() || 'Adet',
      }));
      setRows(draft);
    } catch (err) {
      setParseError(getErrorMessage(err));
    }
  };

  const handleFieldChange = (index: number, field: keyof DraftRow, value: string) => {
    setRows((prev) => prev.map((r, i) => (i === index ? { ...r, [field]: value } : r)));
  };

  const handleRemoveRow = (index: number) => {
    setRows((prev) => prev.filter((_, i) => i !== index));
  };

  const isRowValid = (r: DraftRow) => Boolean(r.name.trim() && r.color.trim() && UNITS.includes(r.unit));

  // Tek bir geçersiz satır tüm yüklemeyi engellememeli — backend zaten geçerli satırları oluşturup
  // geçersizleri ayrı ayrı raporluyor (bulkCreate), bu yüzden burada en az bir geçerli satır yeterli.
  const canUpload = brandId !== null && rows.length > 0 && rows.some(isRowValid);

  const handleUpload = async () => {
    if (!brandId) return;
    setUploading(true);
    setResultMessage(null);
    setRowErrors({});
    try {
      const res = await productApi.bulkCreate({
        companyId,
        BrandId: brandId,
        items: rows.map((r) => ({ name: r.name.trim(), color: r.color.trim(), unit: r.unit })),
      });
      if (res.data.success && res.data.data) {
        const { createdCount, rows: rowResults } = res.data.data;
        setResultMessage({ text: res.data.message || `${createdCount} ürün oluşturuldu.`, error: false });
        const errors: Record<number, string> = {};
        rowResults.forEach((r) => {
          if (!r.success && r.error) errors[r.rowNumber - 1] = r.error;
        });
        setRowErrors(errors);
        if (Object.keys(errors).length === 0) {
          onUploaded();
        }
      } else {
        setResultMessage({ text: res.data.message || 'Yükleme başarısız.', error: true });
      }
    } catch (err) {
      setResultMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setUploading(false);
    }
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>Toplu Ürün Yükle — {brandName}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            CSV formatı: <code>Ürün Adı, Renk, Birim</code> (ilk satır başlık). Barkod her satır için
            otomatik üretilir.
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" startIcon={<DownloadIcon />} onClick={handleDownloadTemplate}>
              Şablon İndir
            </Button>
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv,text/csv"
              hidden
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file) handleFileSelect(file);
              }}
            />
            <Button variant="contained" startIcon={<UploadFileIcon />} onClick={() => fileInputRef.current?.click()}>
              CSV Seç
            </Button>
          </Stack>

          {parseError && <Alert severity="error">{parseError}</Alert>}
          {resultMessage && (
            <Alert severity={resultMessage.error ? 'error' : 'success'}>{resultMessage.text}</Alert>
          )}

          {rows.length > 0 && (
            <Box sx={{ maxHeight: 360, overflow: 'auto' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Ürün Adı</TableCell>
                    <TableCell>Renk</TableCell>
                    <TableCell>Birim</TableCell>
                    <TableCell />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {rows.map((row, index) => (
                    <TableRow key={index}>
                      <TableCell>
                        <TextField
                          size="small"
                          value={row.name}
                          error={!row.name.trim() || !!rowErrors[index]}
                          helperText={rowErrors[index]}
                          onChange={(e) => handleFieldChange(index, 'name', e.target.value)}
                        />
                      </TableCell>
                      <TableCell>
                        <TextField
                          size="small"
                          value={row.color}
                          error={!row.color.trim()}
                          onChange={(e) => handleFieldChange(index, 'color', e.target.value)}
                        />
                      </TableCell>
                      <TableCell>
                        <TextField
                          size="small"
                          select
                          value={row.unit}
                          sx={{ minWidth: 100 }}
                          onChange={(e) => handleFieldChange(index, 'unit', e.target.value)}
                        >
                          {UNITS.map((u) => (
                            <MenuItem key={u} value={u}>
                              {u}
                            </MenuItem>
                          ))}
                        </TextField>
                      </TableCell>
                      <TableCell>
                        <IconButton size="small" onClick={() => handleRemoveRow(index)}>
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={loading || uploading}>
          Kapat
        </Button>
        {rows.length > 0 && (
          <Button variant="contained" disabled={!canUpload || uploading} onClick={handleUpload}>
            {uploading
              ? 'Yükleniyor...'
              : rows.every(isRowValid)
                ? `${rows.length} Ürünü Yükle`
                : `${rows.filter(isRowValid).length}/${rows.length} Geçerli Satırı Yükle`}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
