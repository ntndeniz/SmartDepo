import type { ReactNode } from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';

export interface Column<T> {
  key: string;
  label: string;
  render?: (row: T) => ReactNode;
}

interface Props<T> {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => number | string;
  totalCount: number;
  page: number; // 1 tabanlı
  pageSize: number;
  loading?: boolean;
  search: string;
  onSearchChange: (value: string) => void;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  onAdd?: () => void;
  addLabel?: string;
  toolbar?: ReactNode;
  renderActions?: (row: T) => ReactNode;
}

// Server-side pagination'lı ortak MUI tablosu: veri backend'den sayfa sayfa gelir.
export default function PagedTable<T>({
  columns,
  rows,
  rowKey,
  totalCount,
  page,
  pageSize,
  loading = false,
  search,
  onSearchChange,
  onPageChange,
  onPageSizeChange,
  onAdd,
  addLabel = 'Yeni Ekle',
  toolbar,
  renderActions,
}: Props<T>) {
  return (
    <Paper>
      <Stack direction="row" spacing={2} sx={{ p: 2, alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
        <TextField
          size="small"
          label="Ara"
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          sx={{ minWidth: 220 }}
        />
        {toolbar}
        <Box sx={{ flexGrow: 1 }} />
        {onAdd && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={onAdd}>
            {addLabel}
          </Button>
        )}
      </Stack>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              {columns.map((col) => (
                <TableCell key={col.key} sx={{ fontWeight: 600 }}>
                  {col.label}
                </TableCell>
              ))}
              {renderActions && <TableCell sx={{ fontWeight: 600 }}>İşlemler</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={columns.length + 1} align="center" sx={{ py: 4 }}>
                  <CircularProgress size={28} />
                </TableCell>
              </TableRow>
            ) : rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length + 1} align="center" sx={{ py: 4 }}>
                  Kayıt bulunamadı.
                </TableCell>
              </TableRow>
            ) : (
              rows.map((row) => (
                <TableRow key={rowKey(row)} hover>
                  {columns.map((col) => (
                    <TableCell key={col.key}>
                      {col.render
                        ? col.render(row)
                        : String((row as Record<string, unknown>)[col.key] ?? '')}
                    </TableCell>
                  ))}
                  {renderActions && <TableCell>{renderActions(row)}</TableCell>}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        component="div"
        count={totalCount}
        page={page - 1}
        rowsPerPage={pageSize}
        rowsPerPageOptions={[10, 25, 50]}
        onPageChange={(_, newPage) => onPageChange(newPage + 1)}
        onRowsPerPageChange={(e) => onPageSizeChange(parseInt(e.target.value, 10))}
        labelRowsPerPage="Sayfa boyutu"
        labelDisplayedRows={({ from, to, count }) => `${from}-${to} / ${count}`}
      />
    </Paper>
  );
}
