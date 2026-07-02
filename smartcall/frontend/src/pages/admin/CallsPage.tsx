import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Download, PhoneIncoming } from 'lucide-react';
import { api, getToken } from '../../api/client';
import type { AdminCall } from '../../api/types';

const STATUS_LABELS = ['Waiting', 'In progress', 'Ended'];

export default function CallsPage() {
  const { t } = useTranslation();
  const [calls, setCalls] = useState<AdminCall[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [error, setError] = useState('');

  useEffect(() => {
    api
      .get<{ items: AdminCall[]; total: number }>(`/api/admin/calls?page=${page}&pageSize=20`)
      .then((r) => {
        setCalls(r.items);
        setTotal(r.total);
      })
      .catch((e) => setError(e.message));
  }, [page]);

  return (
    <div>
      <h2>{t('admin.calls')}</h2>
      {error && <div className="error-box">{error}</div>}
      <div className="card" style={{ overflowX: 'auto' }}>
        <table>
          <thead>
            <tr>
              <th>{t('admin.status')}</th>
              <th>Host</th>
              <th>Participants</th>
              <th>Started</th>
              <th>Ended</th>
              <th>{t('admin.recordings')}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {calls.map((c) => (
              <tr key={c.id}>
                <td>
                  <span className={`badge ${c.status === 1 ? 'green' : ''}`}>{STATUS_LABELS[c.status]}</span>
                </td>
                <td>{c.hostName}</td>
                <td>
                  {c.participants.map((p, i) => (
                    <div key={i} style={{ fontSize: 13 }}>
                      {p.displayName}
                      {p.targetLanguageCode && <span className="badge" style={{ marginInlineStart: 6 }}>{p.targetLanguageCode}</span>}
                    </div>
                  ))}
                </td>
                <td>{c.startedAt ? new Date(c.startedAt).toLocaleString() : '—'}</td>
                <td>{c.endedAt ? new Date(c.endedAt).toLocaleString() : '—'}</td>
                <td>
                  {c.recordings.map((r) => (
                    <a
                      key={r.id}
                      href={`/api/recordings/${r.id}/download`}
                      onClick={async (e) => {
                        // authenticated download via fetch → blob
                        e.preventDefault();
                        const res = await fetch(`/api/recordings/${r.id}/download`, {
                          headers: { Authorization: `Bearer ${getToken()}` },
                        });
                        const blob = await res.blob();
                        const url = URL.createObjectURL(blob);
                        window.open(url, '_blank');
                      }}
                      style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 13 }}
                    >
                      <Download size={13} />
                      {(r.fileSizeBytes / 1024 / 1024).toFixed(1)} MB
                    </a>
                  ))}
                </td>
                <td>
                  {c.status === 1 && (
                    // Super admin can join any live call directly, as a full participant.
                    <Link to={`/call/${c.linkCode}`} className="btn secondary" style={{ padding: '6px 12px', fontSize: 13 }}>
                      <PhoneIncoming size={14} />
                      {t('admin.joinCall')}
                    </Link>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {total > 20 && (
          <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
            <button className="btn secondary" disabled={page === 1} onClick={() => setPage(page - 1)}>‹</button>
            <span style={{ alignSelf: 'center', fontSize: 14 }}>{page} / {Math.ceil(total / 20)}</span>
            <button className="btn secondary" disabled={page >= Math.ceil(total / 20)} onClick={() => setPage(page + 1)}>›</button>
          </div>
        )}
      </div>
    </div>
  );
}
