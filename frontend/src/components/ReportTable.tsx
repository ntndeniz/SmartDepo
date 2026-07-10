import { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Chip, IconButton, MenuItem, Snackbar, Stack, TextField } from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import RefreshIcon from '@mui/icons-material/Refresh';
import type { WeeklyReport } from '../types';
import { getErrorMessage } from '../api/client';
import { reportApi } from '../api/services';
import PagedTable, { type Column } from './PagedTable';
import SummaryCards from './SummaryCards';
import { formatDate } from '../utils/formatDate';

interface Props {
  companyId: string;
}

const typeLabel = (t: string) => (t === 'GoodsReceipts' ? 'Mal Kabul' : 'Sevkiyat');

export default function ReportTable({ companyId }: Props) {
  const [rows, setRows] = useState<WeeklyReport[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  const [reportType, setReportType] = useState('');
  // Raporlar günlük değil HAFTALIK bir aralığı temsil ettiği için (WeekStart..WeekEnd), diğer
  // ekranların aksine varsayılan olarak "bugün"e daraltılmaz (neredeyse hiçbir rapor tam bugüne denk
  // gelmeyeceğinden liste boş görünürdü) — boş bırakılırsa tüm raporlar gösterilir, kullanıcı isterse
  // belirli bir tarih aralığıyla (raporun haftası o aralıkla kesişecek şekilde) filtreleyebilir.
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await reportApi.list({
        companyId,
        page,
        pageSize,
        search,
        reportType: reportType || undefined,
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
  }, [companyId, page, pageSize, search, reportType, fromDate, toDate]);

  useEffect(() => {
    const timer = setTimeout(load, search ? 400 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  const handleGenerateNow = async () => {
    setGenerating(true);
    try {
      await reportApi.generateNow({ companyId });
      setMessage({ text: 'Rapor üretimi tamamlandı (eksik hafta varsa eklendi).', error: false });
      await load();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setGenerating(false);
    }
  };

  const handleDownload = async (report: WeeklyReport) => {
    try {
      const res = await reportApi.download(report.id);
      const url = window.URL.createObjectURL(res.data as Blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = report.fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    }
  };

  const columns: Column<WeeklyReport>[] = [
    {
      key: 'reportType',
      label: 'Tür',
      render: (r) => (
        <Chip
          size="small"
          label={typeLabel(r.reportType)}
          color={r.reportType === 'GoodsReceipts' ? 'info' : 'success'}
        />
      ),
    },
    { key: 'weekStart', label: 'Hafta Başı', render: (r) => formatDate(r.weekStart) },
    { key: 'weekEnd', label: 'Hafta Sonu', render: (r) => formatDate(r.weekEnd) },
    { key: 'rowCount', label: 'Satır Sayısı' },
    { key: 'generatedAt', label: 'Üretilme', render: (r) => formatDate(r.generatedAt) },
    { key: 'fileName', label: 'Dosya Adı' },
  ];

  return (
    <>
      <SummaryCards
        cards={[
          { label: 'Toplam Rapor', value: totalCount },
          { label: 'Bu sayfa', value: rows.length },
        ]}
      />
      <PagedTable
        columns={columns}
        rows={rows}
        rowKey={(r) => r.id}
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
        toolbar={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
            <TextField
              size="small"
              select
              label="Tür"
              value={reportType}
              onChange={(e) => {
                setReportType(e.target.value);
                setPage(1);
              }}
              sx={{ minWidth: 160 }}
            >
              <MenuItem value="">Tümü</MenuItem>
              <MenuItem value="GoodsReceipts">Mal Kabul</MenuItem>
              <MenuItem value="Dispatches">Sevkiyat</MenuItem>
            </TextField>
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
            {(fromDate || toDate) && (
              <Button
                size="small"
                onClick={() => {
                  setFromDate('');
                  setToDate('');
                  setPage(1);
                }}
              >
                Filtreyi Temizle
              </Button>
            )}
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              disabled={generating}
              onClick={handleGenerateNow}
            >
              Şimdi Üret
            </Button>
          </Stack>
        }
        renderActions={(r) => (
          <IconButton onClick={() => handleDownload(r)} title="İndir">
            <DownloadIcon />
          </IconButton>
        )}
      />
      <Snackbar
        open={!!message}
        autoHideDuration={4000}
        onClose={() => setMessage(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {message ? (
          <Alert severity={message.error ? 'error' : 'success'} onClose={() => setMessage(null)}>
            {message.text}
          </Alert>
        ) : undefined}
      </Snackbar>
    </>
  );
}
