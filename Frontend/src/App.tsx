import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { MainLayout } from './layouts/MainLayout';
import { HomePage } from './pages/HomePage';
import { CartPage } from './pages/CartPage';
import { PromotionsPage } from './pages/PromotionsPage';
import { OrdersPage } from './pages/OrdersPage';
import { AiMonitoringPage } from './pages/AiMonitoringPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<MainLayout><HomePage /></MainLayout>} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/promotions" element={<MainLayout><PromotionsPage /></MainLayout>} />
        <Route path="/orders" element={<MainLayout><OrdersPage /></MainLayout>} />
        <Route path="/ai-monitoring" element={<MainLayout><AiMonitoringPage /></MainLayout>} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
