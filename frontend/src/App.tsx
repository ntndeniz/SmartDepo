import { useState, useEffect } from 'react';
import {
  AppBar,
  Box,
  Button,
  Container,
  Divider,
  Stack,
  Tab,
  Tabs,
  Toolbar,
  Typography,
} from '@mui/material';
import LogoutIcon from '@mui/icons-material/Logout';
import WarehouseIcon from '@mui/icons-material/Warehouse';
import PeopleIcon from '@mui/icons-material/People';
import LoginScreen from './components/LoginScreen';
import GoodsReceiptTable from './components/GoodsReceiptTable';
import BoxTable from './components/BoxTable';
import LocationTable from './components/LocationTable';
import OrdersTable from './components/OrdersTable';
import ShipmentTable from './components/ShipmentTable';
import ReportTable from './components/ReportTable';
import SettingsPage from './components/SettingsPage';
import UserManagementDialog from './components/UserManagementDialog';

// Markalar ve Mağazalar artık Ayarlar altında alt sekme; KOLI-PALET Konum içine entegre edildi.
// Eski "Dağıtım" sekmesi ikiye ayrıldı: Siparişler (mağaza siparişleri + durumu) ve Sevkiyat
// (koridordan toplama, koli kapatma, palet oluşturma).
const TABS = ['Mal Kabul', 'Konum', 'Siparişler', 'Sevkiyat', 'Raporlar', 'Ayarlar'];

export default function App() {
  const [tab, setTab] = useState(0);
  const [companyId, setCompanyId] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [userDialogOpen, setUserDialogOpen] = useState(false);
  const [role, setRole] = useState<string | null>(null);

  useEffect(() => {
    // Session'dan CompanyId'yi oku
    const storedCompanyId = sessionStorage.getItem('companyId');
    const storedLogin = sessionStorage.getItem('isLoggedIn');
    const storedToken = sessionStorage.getItem('token');
    const storedRole = sessionStorage.getItem('role');

    if (storedCompanyId && storedLogin === 'true' && storedToken) {
      setCompanyId(storedCompanyId);
      setIsLoggedIn(true);
      setRole(storedRole);
    }
  }, []);

  const isAdmin = role === 'Admin';

  const handleLoginSuccess = (newCompanyId: string) => {
    setCompanyId(newCompanyId);
    setIsLoggedIn(true);
    setRole(sessionStorage.getItem('role'));
    setTab(0);
  };

  const handleLogout = () => {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('companyId');
    sessionStorage.removeItem('username');
    sessionStorage.removeItem('role');
    sessionStorage.removeItem('isLoggedIn');
    setCompanyId(null);
    setIsLoggedIn(false);
    setRole(null);
  };

  if (!isLoggedIn || !companyId) {
    return <LoginScreen onLoginSuccess={handleLoginSuccess} />;
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: '#f5f6fa' }}>
      <AppBar position="static">
        <Toolbar>
          <WarehouseIcon sx={{ mr: 1 }} />
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            Akıllı Depo Yönetimi
          </Typography>
          <Typography variant="body2" sx={{ mr: 3, color: 'rgba(255,255,255,0.8)' }}>
            Şirket: {companyId}
          </Typography>
          {isAdmin && (
            <Button
              color="inherit"
              startIcon={<PeopleIcon />}
              onClick={() => setUserDialogOpen(true)}
              size="small"
              sx={{ mr: 1 }}
            >
              Kullanıcılar
            </Button>
          )}
          <Button
            color="inherit"
            startIcon={<LogoutIcon />}
            onClick={handleLogout}
            size="small"
          >
            Çıkış
          </Button>
        </Toolbar>
        <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          textColor="inherit"
          indicatorColor="secondary"
          variant="scrollable"
        >
          {TABS.map((label) => (
            <Tab key={label} label={label} />
          ))}
        </Tabs>
      </AppBar>

      <Container maxWidth="xl" sx={{ py: 3 }}>
        {tab === 0 && <GoodsReceiptTable companyId={companyId} />}
        {tab === 1 && (
          <Stack spacing={4}>
            <Box>
              <Typography variant="h6" sx={{ mb: 1 }}>
                Dinamik Konumlar
              </Typography>
              <LocationTable
                key={`location-${refreshKey}`}
                companyId={companyId}
                onChanged={() => setRefreshKey((k) => k + 1)}
              />
            </Box>
            <Divider />
            <Box>
              <Typography variant="h6" sx={{ mb: 1 }}>
                Stoktaki Koliler
              </Typography>
              <BoxTable
                key={`box-${refreshKey}`}
                companyId={companyId}
                onChanged={() => setRefreshKey((k) => k + 1)}
              />
            </Box>
          </Stack>
        )}
        {tab === 2 && <OrdersTable companyId={companyId} />}
        {tab === 3 && <ShipmentTable companyId={companyId} />}
        {tab === 4 && <ReportTable companyId={companyId} />}
        {tab === 5 && <SettingsPage companyId={companyId} isAdmin={isAdmin} />}
      </Container>

      <UserManagementDialog
        open={userDialogOpen}
        companyId={companyId}
        onClose={() => setUserDialogOpen(false)}
      />
    </Box>
  );
}
