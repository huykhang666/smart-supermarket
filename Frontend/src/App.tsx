import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { MainLayout } from './layouts/MainLayout';
import { HomePage } from './pages/HomePage';
import { CartPage } from './pages/CartPage';
import { PromotionsPage } from './pages/PromotionsPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<MainLayout><HomePage /></MainLayout>} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/promotions" element={<MainLayout><PromotionsPage /></MainLayout>} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
