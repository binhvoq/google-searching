import { useEffect, useMemo, useRef, useState } from 'react';
import type { ChatToolCall } from '../types';
import { API_BASE_URL, chatService } from '../services/api';

type MessageRole = 'user' | 'assistant' | 'tool';

type ChatMessage = {
  id: string;
  role: MessageRole;
  content: string;
  createdAt: number;
};

type ApiCall = {
  id: string;
  name: string;
  status: ChatToolCall['status'];
  detail?: string;
  createdAt: number;
};

const suggestions = [
  'Tìm bệnh viện gần Quận 1',
  'Quán cafe yên tĩnh ở Đà Lạt',
  'Khách sạn 4 sao tại Vũng Tàu',
  'Nhà hàng hải sản ở Nha Trang',
];

function cn(...classes: Array<string | false | null | undefined>) {
  return classes.filter(Boolean).join(' ');
}

function formatTime(ts: number) {
  return new Date(ts).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function MessageBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';
  const isTool = message.role === 'tool';

  return (
    <div className={cn('flex', isUser ? 'justify-end' : 'justify-start')}>
      <div
        className={cn(
          'max-w-[88%] rounded-2xl px-4 py-3 shadow-sm ring-1',
          isUser && 'bg-primary-600 text-white ring-black/5',
          !isUser && !isTool && 'bg-white text-slate-900 ring-black/5',
          isTool && 'bg-slate-900 text-slate-100 ring-black/10',
        )}
      >
        <div className="whitespace-pre-wrap text-sm leading-6">{message.content}</div>
        <div className={cn('mt-1 text-[11px]', isUser ? 'text-white/80' : 'text-slate-500')}>
          {formatTime(message.createdAt)}
        </div>
      </div>
    </div>
  );
}

function ApiStatusPill({ status }: { status: ApiCall['status'] }) {
  const label = status === 'queued' ? 'Chờ' : status === 'running' ? 'Đang chạy' : status === 'done' ? 'Xong' : 'Lỗi';
  const cls =
    status === 'queued'
      ? 'bg-slate-100 text-slate-700'
      : status === 'running'
        ? 'bg-amber-100 text-amber-800'
        : status === 'done'
          ? 'bg-emerald-100 text-emerald-800'
          : 'bg-red-100 text-red-800';
  return <span className={cn('rounded-full px-2 py-0.5 text-xs font-semibold', cls)}>{label}</span>;
}

function getStoredSessionId() {
  try {
    return window.localStorage.getItem('ai_chat_session_id') || '';
  } catch {
    return '';
  }
}

function setStoredSessionId(id: string) {
  try {
    window.localStorage.setItem('ai_chat_session_id', id);
  } catch {
    // ignore
  }
}

export default function ChatWithAI() {
  const [sessionId, setSessionId] = useState<string>(() => getStoredSessionId());
  const [memorySummary, setMemorySummary] = useState<string>('');
  const [input, setInput] = useState('');
  const [autoRunApi, setAutoRunApi] = useState(true);
  const [isSending, setIsSending] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>(() => [
    {
      id: 'm_welcome',
      role: 'assistant',
      createdAt: Date.now(),
      content:
        'Chào bạn! Bạn mô tả nhu cầu bằng tiếng Việt. Mình sẽ tự chọn API phù hợp và chạy để trả kết quả.\n\nGợi ý: “Tìm cafe làm việc ở Quận 3”',
    },
  ]);
  const [apiCalls, setApiCalls] = useState<ApiCall[]>([]);
  const listRef = useRef<HTMLDivElement | null>(null);

  const canSend = input.trim().length > 0 && !isSending;
  const quickChips = useMemo(() => suggestions, []);

  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: 'smooth' });
  }, [messages.length]);

  const send = async () => {
    if (!canSend) return;

    const now = Date.now();
    const userText = input.trim();
    setMessages((prev) => [...prev, { id: `m_${now}`, role: 'user', content: userText, createdAt: now }]);
    setInput('');
    setIsSending(true);

    try {
      const res = await chatService.send({
        sessionId: sessionId || undefined,
        message: userText,
        autoRunApi,
      });

      setSessionId(res.sessionId);
      setStoredSessionId(res.sessionId);
      setMemorySummary(res.memorySummary);

      const ts = Date.now();
      setMessages((prev) => [
        ...prev,
        { id: `m_${ts}`, role: 'assistant', content: res.assistantMessage, createdAt: ts },
      ]);

      if (res.toolCalls?.length) {
        const toolEntries: ApiCall[] = res.toolCalls.map((t, idx) => ({
          id: `${res.sessionId}_${ts}_${idx}`,
          name: t.name,
          status: t.status as ApiCall['status'],
          detail: t.detail,
          createdAt: ts,
        }));
        setApiCalls((prev) => [...toolEntries, ...prev].slice(0, 20));
      }
    } catch (e: any) {
      const msg = e?.response?.data?.error || e?.message || 'Không thể gọi API chat. Kiểm tra backend và cấu hình Azure OpenAI.';
      const ts = Date.now();
      setMessages((prev) => [
        ...prev,
        { id: `m_${ts}`, role: 'assistant', content: `Có lỗi: ${msg}`, createdAt: ts },
      ]);
    } finally {
      setIsSending(false);
    }
  };

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <div className="lg:col-span-2">
        <div className="rounded-2xl bg-white/70 backdrop-blur-md shadow-lg ring-1 ring-black/5">
          <div className="flex flex-col gap-3 border-b border-black/5 px-5 py-4 md:flex-row md:items-center md:justify-between">
            <div>
              <h2 className="text-lg font-bold text-slate-900">Chat với A.I</h2>
              <p className="text-sm text-slate-600">Azure OpenAI (gpt-4o-mini) + tool gọi API tìm kiếm địa điểm.</p>
              <p className="mt-1 text-xs text-slate-400">API: {API_BASE_URL}</p>
            </div>
            <label className="inline-flex items-center gap-3 rounded-xl bg-white/70 px-4 py-2 text-sm font-semibold text-slate-800 ring-1 ring-black/5">
              <span>Tự chạy API</span>
              <button
                type="button"
                onClick={() => setAutoRunApi((v) => !v)}
                className={cn('relative h-6 w-11 rounded-full transition', autoRunApi ? 'bg-primary-600' : 'bg-slate-300')}
                aria-pressed={autoRunApi}
              >
                <span
                  className={cn(
                    'absolute top-0.5 h-5 w-5 rounded-full bg-white shadow-sm transition',
                    autoRunApi ? 'left-5' : 'left-0.5',
                  )}
                />
              </button>
            </label>
          </div>

          <div ref={listRef} className="h-[420px] overflow-auto px-5 py-5 scrollbar-slim">
            <div className="space-y-3">
              {messages.map((m) => (
                <MessageBubble key={m.id} message={m} />
              ))}
            </div>
          </div>

          <div className="border-t border-black/5 px-5 py-4">
            <div className="mb-3 flex flex-wrap gap-2">
              {quickChips.map((chip) => (
                <button
                  key={chip}
                  type="button"
                  onClick={() => setInput(chip)}
                  className="rounded-full bg-white/70 px-3 py-1 text-sm text-slate-700 ring-1 ring-black/5 hover:bg-white"
                >
                  {chip}
                </button>
              ))}
            </div>

            <div className="flex gap-3">
              <div className="flex-1">
                <textarea
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                      e.preventDefault();
                      void send();
                    }
                  }}
                  placeholder="Nhập yêu cầu... (vd: Tìm bệnh viện gần Quận 1, có đánh giá tốt)"
                  rows={2}
                  className="w-full resize-none rounded-xl border border-black/10 bg-white px-4 py-3 text-sm leading-6 text-slate-900 outline-none ring-0 transition focus:border-primary-300 focus:ring-4 focus:ring-primary-100 disabled:bg-slate-50"
                  disabled={isSending}
                />
                <div className="mt-1 text-xs text-slate-500">Enter để xuống dòng. Ctrl+Enter để gửi nhanh.</div>
              </div>
              <button
                type="button"
                onClick={() => void send()}
                disabled={!canSend}
                className="h-fit rounded-xl bg-primary-600 px-5 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isSending ? 'Đang gửi...' : 'Gửi'}
              </button>
            </div>
          </div>
        </div>
      </div>

      <aside className="space-y-6">
        <div className="rounded-2xl bg-white/70 backdrop-blur-md shadow-lg ring-1 ring-black/5">
          <div className="border-b border-black/5 px-5 py-4">
            <h3 className="text-sm font-bold text-slate-900">API đã gọi</h3>
            <p className="mt-1 text-sm text-slate-600">Hiển thị tool calls mà A.I thực hiện.</p>
          </div>
          <div className="px-5 py-4">
            {apiCalls.length === 0 ? (
              <div className="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-600 ring-1 ring-black/5">
                Chưa có API nào được gọi.
              </div>
            ) : (
              <div className="space-y-3">
                {apiCalls.map((c) => (
                  <div key={c.id} className="rounded-xl bg-white px-4 py-3 ring-1 ring-black/5">
                    <div className="flex items-center justify-between gap-3">
                      <div className="min-w-0">
                        <div className="truncate text-sm font-semibold text-slate-900">{c.name}</div>
                        <div className="mt-1 text-xs text-slate-500">
                          {formatTime(c.createdAt)}
                          {c.detail ? ` • ${c.detail}` : ''}
                        </div>
                      </div>
                      <ApiStatusPill status={c.status} />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        <div className="rounded-2xl bg-white/70 backdrop-blur-md shadow-lg ring-1 ring-black/5">
          <div className="border-b border-black/5 px-5 py-4">
            <h3 className="text-sm font-bold text-slate-900">Trích nhớ</h3>
            <p className="mt-1 text-sm text-slate-600">Thông tin ngắn gọn được lưu trong session.</p>
          </div>
          <div className="px-5 py-4">
            {memorySummary ? (
              <pre className="whitespace-pre-wrap rounded-xl bg-slate-50 px-4 py-3 text-xs text-slate-700 ring-1 ring-black/5">
                {memorySummary}
              </pre>
            ) : (
              <div className="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-600 ring-1 ring-black/5">
                Chưa có trích nhớ.
              </div>
            )}
          </div>
        </div>

        <div className="rounded-2xl bg-white/70 backdrop-blur-md shadow-lg ring-1 ring-black/5">
          <div className="border-b border-black/5 px-5 py-4">
            <h3 className="text-sm font-bold text-slate-900">Mẹo prompt</h3>
            <p className="mt-1 text-sm text-slate-600">Ghi rõ khu vực, loại địa điểm, và tiêu chí.</p>
          </div>
          <div className="px-5 py-4 text-sm text-slate-700">
            <ul className="list-disc space-y-2 pl-5">
              <li>“Tìm quán cafe làm việc ở Quận 3, có wifi và yên tĩnh.”</li>
              <li>“Gợi ý 10 nhà hàng chay ở Đà Nẵng, đánh giá &gt; 4.2.”</li>
              <li>“Tìm bệnh viện gần Thủ Đức, ưu tiên gần trạm metro.”</li>
            </ul>
          </div>
        </div>
      </aside>
    </div>
  );
}
