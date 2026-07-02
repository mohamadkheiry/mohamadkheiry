import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Coins } from 'lucide-react';
import { api } from '../../api/client';
import type { AdminUser, TokenUsageReport } from '../../api/types';

export default function UsersPage() {
  const { t } = useTranslation();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [report, setReport] = useState<TokenUsageReport | null>(null);
  const [error, setError] = useState('');

  const load = () => {
    api.get<{ items: AdminUser[]; total: number }>('/api/admin/users?page=1&pageSize=100')
      .then((r) => setUsers(r.items))
      .catch((e) => setError(e.message));
    api.get<TokenUsageReport>('/api/admin/token-usage').then(setReport).catch(() => {});
  };

  useEffect(load, []);

  const toggleActive = async (u: AdminUser) => {
    await api.put(`/api/admin/users/${u.id}/active`, { isActive: !u.isActive });
    load();
  };

  return (
    <div>
      <h2>{t('admin.users')}</h2>
      {error && <div className="error-box">{error}</div>}

      {report && (
        <div className="card" style={{ marginBottom: 18, display: 'flex', alignItems: 'center', gap: 12 }}>
          <Coins size={22} color="#4f7cff" />
          <strong>{t('admin.totalTokens')}:</strong>
          <span dir="ltr">{report.systemTotalTokens.toLocaleString()}</span>
        </div>
      )}

      <div className="card" style={{ overflowX: 'auto', marginBottom: 18 }}>
        <table>
          <thead>
            <tr>
              <th>{t('auth.email')}</th>
              <th>{t('auth.displayName')}</th>
              <th>{t('admin.status')}</th>
              <th>Tokens</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td dir="ltr">{u.email}</td>
                <td>{u.displayName} {u.isSuperAdmin && <span className="badge green">admin</span>}</td>
                <td>
                  <span className={`badge ${u.isActive ? 'green' : 'red'}`}>
                    {u.isActive ? t('admin.active') : t('admin.inactive')}
                  </span>
                </td>
                <td dir="ltr">{u.totalTokensUsed.toLocaleString()}</td>
                <td>
                  {!u.isSuperAdmin && (
                    <button className="btn secondary" style={{ padding: '5px 12px', fontSize: 13 }} onClick={() => toggleActive(u)}>
                      {u.isActive ? t('admin.inactive') : t('admin.active')}
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {report && (
        <div className="grid-2">
          <div className="card" style={{ overflowX: 'auto' }}>
            <h3>{t('admin.byUser')}</h3>
            <table>
              <thead><tr><th>{t('auth.email')}</th><th>Input</th><th>Output</th><th>Total</th></tr></thead>
              <tbody>
                {report.byUser.map((r, i) => (
                  <tr key={i}>
                    <td dir="ltr">{r.email ?? '—'}</td>
                    <td dir="ltr">{r.inputTokens.toLocaleString()}</td>
                    <td dir="ltr">{r.outputTokens.toLocaleString()}</td>
                    <td dir="ltr">{r.totalTokens.toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="card" style={{ overflowX: 'auto' }}>
            <h3>{t('admin.byCall')}</h3>
            <table>
              <thead><tr><th>Call</th><th>Total</th></tr></thead>
              <tbody>
                {report.byCall.map((r, i) => (
                  <tr key={i}>
                    <td dir="ltr">{r.linkCode ?? '—'}</td>
                    <td dir="ltr">{r.totalTokens.toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
