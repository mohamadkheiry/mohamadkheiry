import { FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2 } from 'lucide-react';
import { api } from '../../api/client';
import type { AdminLanguage } from '../../api/types';

const EMPTY = { code: '', englishName: '', nativeName: '', isRtl: false, isActive: true, sortOrder: 0 };

export default function LanguagesPage() {
  const { t } = useTranslation();
  const [languages, setLanguages] = useState<AdminLanguage[]>([]);
  const [form, setForm] = useState(EMPTY);
  const [error, setError] = useState('');

  const load = () => {
    api.get<AdminLanguage[]>('/api/admin/languages').then(setLanguages).catch((e) => setError(e.message));
  };

  useEffect(load, []);

  const add = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await api.post('/api/admin/languages', { id: null, ...form });
      setForm(EMPTY);
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    }
  };

  const toggle = async (l: AdminLanguage) => {
    await api.post('/api/admin/languages', { ...l, isActive: !l.isActive });
    load();
  };

  const remove = async (l: AdminLanguage) => {
    await api.del(`/api/admin/languages/${l.id}`);
    load();
  };

  return (
    <div style={{ maxWidth: 820 }}>
      <h2>{t('admin.languages')}</h2>
      {error && <div className="error-box">{error}</div>}

      <form onSubmit={add} className="card" style={{ marginBottom: 18 }}>
        <h3>{t('admin.addLanguage')}</h3>
        <div className="grid-2">
          <div className="field">
            <label>{t('admin.code')}</label>
            <input dir="ltr" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} required maxLength={10} />
          </div>
          <div className="field">
            <label>{t('admin.englishName')}</label>
            <input dir="ltr" value={form.englishName} onChange={(e) => setForm({ ...form, englishName: e.target.value })} required />
          </div>
          <div className="field">
            <label>{t('admin.nativeName')}</label>
            <input value={form.nativeName} onChange={(e) => setForm({ ...form, nativeName: e.target.value })} required />
          </div>
          <div className="field" style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
            <input type="checkbox" id="rtl" checked={form.isRtl} onChange={(e) => setForm({ ...form, isRtl: e.target.checked })} style={{ width: 'auto' }} />
            <label htmlFor="rtl" style={{ color: 'var(--text)' }}>{t('admin.rtl')}</label>
          </div>
        </div>
        <button className="btn" type="submit">
          <Plus size={16} />
          {t('admin.addLanguage')}
        </button>
      </form>

      <div className="card" style={{ overflowX: 'auto' }}>
        <table>
          <thead>
            <tr>
              <th>{t('admin.code')}</th>
              <th>{t('admin.englishName')}</th>
              <th>{t('admin.nativeName')}</th>
              <th>{t('admin.status')}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {languages.map((l) => (
              <tr key={l.id}>
                <td dir="ltr">{l.code}</td>
                <td dir="ltr">{l.englishName}</td>
                <td>{l.nativeName}</td>
                <td>
                  <button className={`badge ${l.isActive ? 'green' : 'red'}`} style={{ cursor: 'pointer', background: 'transparent' }} onClick={() => toggle(l)}>
                    {l.isActive ? t('admin.active') : t('admin.inactive')}
                  </button>
                </td>
                <td>
                  <button className="btn ghost" onClick={() => remove(l)} title="Delete">
                    <Trash2 size={15} color="#ef4757" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
