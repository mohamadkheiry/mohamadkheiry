import { useEffect, useState } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { api } from './api/client';
import { useAuth } from './store/auth';
import Landing from './pages/Landing';
import Login from './pages/Login';
import Register from './pages/Register';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import Dashboard from './pages/Dashboard';
import CallRoom from './pages/CallRoom';
import Installer from './pages/Installer';
import AdminLayout from './pages/admin/AdminLayout';
import AiSettingsPage from './pages/admin/AiSettingsPage';
import GeneralSettingsPage from './pages/admin/GeneralSettingsPage';
import CallsPage from './pages/admin/CallsPage';
import UsersPage from './pages/admin/UsersPage';
import FontsPage from './pages/admin/FontsPage';
import LandingContentPage from './pages/admin/LandingContentPage';
import SmtpPage from './pages/admin/SmtpPage';
import LanguagesPage from './pages/admin/LanguagesPage';

export default function App() {
  const { user } = useAuth();
  const [installed, setInstalled] = useState<boolean | null>(null);

  useEffect(() => {
    api
      .get<{ installed: boolean }>('/api/public/install-status')
      .then((r) => setInstalled(r.installed))
      .catch(() => setInstalled(true)); // if the check fails, don't block the app
  }, []);

  if (installed === null) return null;
  if (!installed) return <Installer onInstalled={() => setInstalled(true)} />;

  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/reset-password" element={<ResetPassword />} />
      <Route path="/dashboard" element={user ? <Dashboard /> : <Navigate to="/login" />} />
      <Route path="/call/:linkCode" element={<CallRoom />} />
      <Route path="/install" element={<Installer onInstalled={() => setInstalled(true)} />} />
      <Route path="/admin" element={user?.isSuperAdmin ? <AdminLayout /> : <Navigate to="/login" />}>
        <Route index element={<Navigate to="ai" replace />} />
        <Route path="ai" element={<AiSettingsPage />} />
        <Route path="general" element={<GeneralSettingsPage />} />
        <Route path="calls" element={<CallsPage />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="fonts" element={<FontsPage />} />
        <Route path="landing" element={<LandingContentPage />} />
        <Route path="smtp" element={<SmtpPage />} />
        <Route path="languages" element={<LanguagesPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" />} />
    </Routes>
  );
}
