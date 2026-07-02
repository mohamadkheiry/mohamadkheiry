import { NavLink, Outlet } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Bot, Globe, PhoneCall, Users, Type, LayoutTemplate, Mail, Languages,
} from 'lucide-react';
import TopBar from '../../components/TopBar';

const ITEMS = [
  { to: 'ai', icon: Bot, key: 'admin.ai' },
  { to: 'general', icon: Globe, key: 'admin.general' },
  { to: 'languages', icon: Languages, key: 'admin.languages' },
  { to: 'calls', icon: PhoneCall, key: 'admin.calls' },
  { to: 'users', icon: Users, key: 'admin.users' },
  { to: 'fonts', icon: Type, key: 'admin.fonts' },
  { to: 'landing', icon: LayoutTemplate, key: 'admin.landing' },
  { to: 'smtp', icon: Mail, key: 'admin.smtp' },
];

export default function AdminLayout() {
  const { t } = useTranslation();

  return (
    <>
      <TopBar />
      <div className="admin-layout">
        <aside className="admin-sidebar">
          {ITEMS.map(({ to, icon: Icon, key }) => (
            <NavLink key={to} to={to} className={({ isActive }) => (isActive ? 'active' : '')}>
              <Icon size={18} />
              {t(key)}
            </NavLink>
          ))}
        </aside>
        <section className="admin-content">
          <Outlet />
        </section>
      </div>
    </>
  );
}
