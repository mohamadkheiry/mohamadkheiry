import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Video, LogOut, ShieldCheck, LayoutDashboard, Globe } from 'lucide-react';
import { useAuth } from '../store/auth';

export default function TopBar() {
  const { t, i18n } = useTranslation();
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const switchLang = () => i18n.changeLanguage(i18n.language === 'fa' ? 'en' : 'fa');

  return (
    <header className="topbar">
      <Link to="/" className="brand">
        <Video size={24} color="#4f7cff" />
        {t('app.name')}
      </Link>
      <nav>
        <button className="btn ghost" onClick={switchLang} title={t('common.language')}>
          <Globe size={18} />
          {i18n.language === 'fa' ? 'EN' : 'فا'}
        </button>
        {user ? (
          <>
            <Link to="/dashboard" className="btn secondary">
              <LayoutDashboard size={16} />
              {t('nav.dashboard')}
            </Link>
            {user.isSuperAdmin && (
              <Link to="/admin" className="btn secondary">
                <ShieldCheck size={16} />
                {t('nav.admin')}
              </Link>
            )}
            <button
              className="btn ghost"
              onClick={() => {
                logout();
                navigate('/');
              }}
            >
              <LogOut size={16} />
              {t('nav.logout')}
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="btn ghost">
              {t('nav.login')}
            </Link>
            <Link to="/register" className="btn">
              {t('nav.register')}
            </Link>
          </>
        )}
      </nav>
    </header>
  );
}
