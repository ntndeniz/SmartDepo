import { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Snackbar,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import SettingsIcon from '@mui/icons-material/Settings';
import { getErrorMessage } from '../api/client';
import { companySettingsApi } from '../api/services';
import { formatDate } from '../utils/formatDate';
import BrandTable from './BrandTable';
import StoreTable from './StoreTable';

interface Props {
  companyId: string;
  isAdmin: boolean;
}

const SUB_TABS = ['Depo Boyutu', 'Markalar', 'Mağazalar'];

function WarehouseSizePanel({ companyId, isAdmin }: Props) {
  const [corridorCount, setCorridorCount] = useState(2);
  const [zonesPerCorridor, setZonesPerCorridor] = useState(2);
  const [shelvesPerZone, setShelvesPerZone] = useState(2);
  const [isConfigured, setIsConfigured] = useState(false);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const res = await companySettingsApi.get();
      if (res.data.success && res.data.data) {
        const s = res.data.data;
        setIsConfigured(s.isConfigured);
        setUpdatedAt(s.updatedAt);
        if (s.isConfigured) {
          setCorridorCount(s.corridorCount);
          setZonesPerCorridor(s.zonesPerCorridor);
          setShelvesPerZone(s.shelvesPerZone);
        }
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [companyId]);

  const handleSave = async () => {
    setSaving(true);
    try {
      const res = await companySettingsApi.update({
        companyId,
        CorridorCount: corridorCount,
        ZonesPerCorridor: zonesPerCorridor,
        ShelvesPerZone: shelvesPerZone,
      });
      if (res.data.success && res.data.data) {
        setMessage({ text: res.data.message || 'Kaydedildi.', error: false });
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu.', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const totalLocations = corridorCount * zonesPerCorridor * shelvesPerZone;

  return (
    <Box sx={{ maxWidth: 640 }}>
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" sx={{ mb: 1 }}>
            Depo Boyutu
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Konum ekranındaki "Toplu Üretim" bu değerleri kullanır; her seferinde yeniden girmenize
            gerek kalmaz. Değiştirdiğinizde yalnızca eksik konumlar eklenir, var olanlar silinmez.
          </Typography>

          {!isConfigured && !loading && (
            <Alert severity="info" sx={{ mb: 2 }}>
              Depo boyutu henüz ayarlanmadı. Aşağıdaki değerleri girip kaydedin.
            </Alert>
          )}
          {!isAdmin && (
            <Alert severity="warning" sx={{ mb: 2 }}>
              Depo boyutunu yalnızca yöneticiler değiştirebilir.
            </Alert>
          )}

          <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
            <TextField
              label="Koridor Sayısı"
              type="number"
              value={corridorCount}
              onChange={(e) => setCorridorCount(Math.max(1, parseInt(e.target.value, 10) || 1))}
              fullWidth
              disabled={!isAdmin}
              slotProps={{ htmlInput: { min: 1 } }}
            />
            <TextField
              label="Koridor Başına Bölge"
              type="number"
              value={zonesPerCorridor}
              onChange={(e) => setZonesPerCorridor(Math.max(1, parseInt(e.target.value, 10) || 1))}
              fullWidth
              disabled={!isAdmin}
              slotProps={{ htmlInput: { min: 1 } }}
            />
            <TextField
              label="Bölge Başına Raf"
              type="number"
              value={shelvesPerZone}
              onChange={(e) => setShelvesPerZone(Math.max(1, parseInt(e.target.value, 10) || 1))}
              fullWidth
              disabled={!isAdmin}
              slotProps={{ htmlInput: { min: 1 } }}
            />
          </Stack>

          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Toplam konum sayısı: <strong>{totalLocations}</strong>
            {updatedAt && ` — son güncelleme: ${formatDate(updatedAt)}`}
          </Typography>

          <Button variant="contained" disabled={!isAdmin || saving || loading} onClick={handleSave}>
            {saving ? 'Kaydediliyor...' : 'Kaydet'}
          </Button>
        </CardContent>
      </Card>

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
    </Box>
  );
}

export default function SettingsPage({ companyId, isAdmin }: Props) {
  const [subTab, setSubTab] = useState(0);

  return (
    <Box>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 2 }}>
        <SettingsIcon color="primary" />
        <Typography variant="h6">Firma Ayarları</Typography>
      </Stack>

      <Tabs
        value={subTab}
        onChange={(_, value) => setSubTab(value)}
        sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}
      >
        {SUB_TABS.map((label) => (
          <Tab key={label} label={label} />
        ))}
      </Tabs>

      {subTab === 0 && <WarehouseSizePanel companyId={companyId} isAdmin={isAdmin} />}
      {subTab === 1 && <BrandTable companyId={companyId} isAdmin={isAdmin} />}
      {subTab === 2 && <StoreTable companyId={companyId} isAdmin={isAdmin} />}
    </Box>
  );
}
