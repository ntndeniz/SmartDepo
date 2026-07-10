import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Paper,
  Snackbar,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import Inventory2Icon from '@mui/icons-material/Inventory2';
import CloudDownloadIcon from '@mui/icons-material/CloudDownload';
import DeleteIcon from '@mui/icons-material/Delete';
import OutputIcon from '@mui/icons-material/Output';
import type { Location } from '../types';
import { getErrorMessage } from '../api/client';
import { locationApi } from '../api/services';
import DeleteConfirmModal from './DeleteConfirmModal';
import SummaryCards from './SummaryCards';
import BarcodeScannerModal from './BarcodeScannerModal';
import PrintBarcodeButton from './PrintBarcodeButton';

interface Props {
  companyId: string;
  onChanged?: () => void;
}

// Depo haritasını tek seferde göstermek için (sayfalama yerine) tüm konumlar çekilir. Sabit bir üst
// sınır (ör. eski MAP_PAGE_SIZE=1000) kullanmak, o sayıyı aşan şirketlerde konumların hiçbir hata
// vermeden sessizce gizlenmesine yol açıyordu — bu yüzden burada totalCount'a göre gereken kadar
// sayfa istenir, hiçbir konum atlanmaz.
const FETCH_PAGE_SIZE = 500;

export default function LocationTable({ companyId, onChanged }: Props) {
  const [rows, setRows] = useState<Location[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);

  const [selectedCorridor, setSelectedCorridor] = useState<number | null>(null);
  const [selected, setSelected] = useState<Location | null>(null);
  const [boxBarcode, setBoxBarcode] = useState('');
  const [scannerOpen, setScannerOpen] = useState(false);
  const [deleting, setDeleting] = useState<Location | null>(null);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const first = await locationApi.list({ companyId, page: 1, pageSize: FETCH_PAGE_SIZE, search });
      let all = first.data;
      const totalPages = Math.ceil(first.totalCount / FETCH_PAGE_SIZE);
      for (let p = 2; p <= totalPages; p++) {
        const res = await locationApi.list({ companyId, page: p, pageSize: FETCH_PAGE_SIZE, search });
        all = all.concat(res.data);
      }
      setRows(all);
      setTotalCount(first.totalCount);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
  }, [companyId, search]);

  useEffect(() => {
    const timer = setTimeout(load, search ? 400 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  const handleGenerate = async () => {
    setGenerating(true);
    try {
      const res = await locationApi.generate({ companyId });
      if (res.data.success && res.data.data) {
        const data = res.data.data;
        setMessage({
          text: `${data.createdCount} yeni konum oluşturuldu (toplam ${data.totalCount}).`,
          error: false,
        });
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu. Önce Ayarlar\'dan depo boyutunu girin.', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setGenerating(false);
    }
  };

  const handleAssignBox = async (barcode: string) => {
    if (!selected) return;
    setSaving(true);
    try {
      const res = await locationApi.assignBox({ companyId, LocationId: selected.id, BoxBarcode: barcode });
      if (res.data.success) {
        setMessage({ text: 'Koli rafa yerleştirildi.', error: false });
        setSelected(null);
        setBoxBarcode('');
        setScannerOpen(false);
        await load();
        onChanged?.();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleRelease = async (location: Location) => {
    setSaving(true);
    try {
      const res = await locationApi.release({ companyId, LocationId: location.id });
      if (res.data.success) {
        setMessage({ text: 'Konum boşaltıldı.', error: false });
        setSelected(null);
        await load();
        onChanged?.();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
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
      const res = await locationApi.delete({ id: deleting.id, companyId });
      if (res.data.success) {
        setMessage({ text: 'Konum silindi.', error: false });
        setDeleting(null);
        setSelected(null);
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

  const occupied = rows.filter((r) => r.isOccupied).length;
  const empty = rows.filter((r) => !r.isOccupied).length;

  // Koridor → Bölge → Raf yerleşimini görsel bir depo haritası olarak grupla.
  const corridors = useMemo(() => {
    const byCorridor = new Map<number, Map<number, Location[]>>();
    for (const loc of rows) {
      if (!byCorridor.has(loc.corridorNo)) byCorridor.set(loc.corridorNo, new Map());
      const zones = byCorridor.get(loc.corridorNo)!;
      if (!zones.has(loc.zoneNo)) zones.set(loc.zoneNo, []);
      zones.get(loc.zoneNo)!.push(loc);
    }
    return Array.from(byCorridor.entries())
      .sort((a, b) => a[0] - b[0])
      .map(([corridorNo, zonesMap]) => ({
        corridorNo,
        zones: Array.from(zonesMap.entries())
          .sort((a, b) => a[0] - b[0])
          .map(([zoneNo, shelves]) => ({
            zoneNo,
            shelves: shelves.sort((a, b) => a.shelfNo - b.shelfNo),
          })),
      }));
  }, [rows]);

  // Depo çok büyükse tüm koridorları aynı anda alt alta göstermek sayfayı kilometrelerce uzatıyordu —
  // artık bir koridor dropdown'dan seçilir, yalnızca o koridorun bölge/raf haritası gösterilir.
  useEffect(() => {
    if (corridors.length === 0) {
      setSelectedCorridor(null);
      return;
    }
    if (!corridors.some((c) => c.corridorNo === selectedCorridor)) {
      setSelectedCorridor(corridors[0].corridorNo);
    }
  }, [corridors, selectedCorridor]);

  const activeCorridor = corridors.find((c) => c.corridorNo === selectedCorridor) ?? null;

  return (
    <>
      <SummaryCards
        cards={[
          { label: 'Toplam Konum', value: totalCount },
          { label: 'Dolu', value: occupied, color: '#c62828' },
          { label: 'Boş', value: empty, color: '#2e7d32' },
        ]}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 3, alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
        <TextField
          size="small"
          label="Konum barkodu ara"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ minWidth: 220 }}
        />
        {corridors.length > 0 && (
          <TextField
            size="small"
            select
            label="Koridor"
            value={selectedCorridor ?? ''}
            onChange={(e) => setSelectedCorridor(Number(e.target.value))}
            sx={{ minWidth: 160 }}
          >
            {corridors.map(({ corridorNo, zones }) => (
              <MenuItem key={corridorNo} value={corridorNo}>
                Koridor {corridorNo} ({zones.reduce((sum, z) => sum + z.shelves.length, 0)} raf)
              </MenuItem>
            ))}
          </TextField>
        )}
        <Box sx={{ flexGrow: 1 }} />
        <Button variant="contained" startIcon={<CloudDownloadIcon />} disabled={generating} onClick={handleGenerate}>
          {generating ? 'Üretiliyor...' : 'Toplu Üretim'}
        </Button>
      </Stack>

      {loading ? (
        <Typography color="text.secondary">Yükleniyor...</Typography>
      ) : corridors.length === 0 ? (
        <Alert severity="info">
          Henüz konum tanımlı değil. "Toplu Üretim" ile Ayarlar'da belirlediğiniz depo boyutuna göre
          konumlar otomatik oluşturulur.
        </Alert>
      ) : activeCorridor ? (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
            Koridor {activeCorridor.corridorNo}
          </Typography>
          <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap', gap: 2 }}>
            {activeCorridor.zones.map(({ zoneNo, shelves }) => (
              <Box
                key={zoneNo}
                sx={{
                  border: '1px solid',
                  borderColor: 'divider',
                  borderRadius: 2,
                  p: 1.5,
                  minWidth: 160,
                  bgcolor: '#fafafa',
                }}
              >
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                  Bölge {zoneNo}
                </Typography>
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                  {shelves.map((loc) => (
                    <Tooltip
                      key={loc.id}
                      title={
                        loc.isOccupied
                          ? `${loc.barcode} — ${loc.currentBoxBarcode ?? ''} (${loc.currentBoxProductName ?? ''})`
                          : `${loc.barcode} — boş`
                      }
                    >
                      <Box
                        onClick={() => setSelected(loc)}
                        sx={{
                          width: 56,
                          height: 56,
                          borderRadius: 1.5,
                          display: 'flex',
                          flexDirection: 'column',
                          alignItems: 'center',
                          justifyContent: 'center',
                          cursor: 'pointer',
                          bgcolor: loc.isOccupied ? '#ffebee' : '#e8f5e9',
                          border: '1px solid',
                          borderColor: loc.isOccupied ? '#ef9a9a' : '#a5d6a7',
                          transition: 'transform 0.1s',
                          '&:hover': { transform: 'scale(1.06)' },
                        }}
                      >
                        {loc.isOccupied ? (
                          <Inventory2Icon fontSize="small" sx={{ color: '#c62828' }} />
                        ) : null}
                        <Typography variant="caption" sx={{ fontWeight: 600 }}>
                          R{loc.shelfNo}
                        </Typography>
                      </Box>
                    </Tooltip>
                  ))}
                </Stack>
              </Box>
            ))}
          </Stack>
        </Paper>
      ) : null}

      {/* Konum detay/işlem diyaloğu */}
      <Dialog open={selected !== null} onClose={() => setSelected(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Konum — {selected?.barcode}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography variant="body2" color="text.secondary">
              Koridor {selected?.corridorNo} / Bölge {selected?.zoneNo} / Raf {selected?.shelfNo}
            </Typography>
            <Chip
              label={selected?.isOccupied ? 'Dolu' : 'Boş'}
              color={selected?.isOccupied ? 'error' : 'success'}
              sx={{ alignSelf: 'flex-start' }}
            />

            {selected?.isOccupied ? (
              <Typography variant="body2">
                Koli: <strong>{selected.currentBoxBarcode}</strong>
                {selected.currentBoxProductName ? ` (${selected.currentBoxProductName})` : ''}
              </Typography>
            ) : (
              <>
                <TextField
                  label="Koli Barkodu"
                  value={boxBarcode}
                  onChange={(e) => setBoxBarcode(e.target.value)}
                  fullWidth
                  autoFocus
                />
                <Button variant="outlined" onClick={() => setScannerOpen(true)}>
                  Barkod Tara
                </Button>
              </>
            )}

            {selected && <PrintBarcodeButton value={selected.barcode} label={`Koridor ${selected.corridorNo} / Bölge ${selected.zoneNo} / Raf ${selected.shelfNo}`} />}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelected(null)} disabled={saving}>
            Kapat
          </Button>
          {selected?.isOccupied ? (
            <Button
              variant="contained"
              color="warning"
              startIcon={<OutputIcon />}
              disabled={saving}
              onClick={() => selected && handleRelease(selected)}
            >
              Koliyi Kaldır
            </Button>
          ) : (
            <>
              <Button
                variant="outlined"
                color="error"
                startIcon={<DeleteIcon />}
                disabled={saving}
                onClick={() => selected && setDeleting(selected)}
              >
                Sil
              </Button>
              <Button
                variant="contained"
                disabled={saving || !boxBarcode.trim()}
                onClick={() => handleAssignBox(boxBarcode)}
              >
                Yerleştir
              </Button>
            </>
          )}
        </DialogActions>
      </Dialog>

      <DeleteConfirmModal
        open={deleting !== null}
        loading={saving}
        onConfirm={handleDelete}
        onClose={() => setDeleting(null)}
        title="Konum Sil"
        message={`Konum ${deleting?.barcode} silinecektir. Emin misiniz?`}
      />

      <BarcodeScannerModal
        open={scannerOpen}
        onClose={() => setScannerOpen(false)}
        onScan={(barcode) => {
          setBoxBarcode(barcode);
          setScannerOpen(false);
        }}
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
