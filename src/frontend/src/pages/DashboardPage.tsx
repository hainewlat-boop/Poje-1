import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { usersApi } from '../services/api';

export default function DashboardPage() {
  const { user, setUser } = useAuthStore();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!user) {
      usersApi.getMe()
        .then((userData) => {
          setUser(userData);
        })
        .catch(console.error)
        .finally(() => setLoading(false));
    } else {
      setLoading(false);
    }
  }, [user, setUser]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  const stats = [
    { name: 'Aktif Başvurular', value: '12', icon: '📋', color: 'bg-blue-500' },
    { name: 'Bekleyen Ödemeler', value: '3', icon: '💳', color: 'bg-yellow-500' },
    { name: 'Tamamlanan', value: '45', icon: '✅', color: 'bg-green-500' },
    { name: 'Onay Bekleyen', value: '5', icon: '⏳', color: 'bg-orange-500' },
  ];

  const quickActions = [
    { name: 'Yeni Başvuru', href: '/applications/new', icon: '➕', description: 'Yeni başvuru oluştur' },
    { name: 'Ödeme Yap', href: '/payments', icon: '💰', description: 'Bekleyen ödemeleri gör' },
    { name: 'Belgeler', href: '/documents', icon: '📄', description: 'Belge ve dekontlar' },
    { name: 'Destek', href: '/support', icon: '❓', description: 'Yardım ve destek' },
  ];

  return (
    <div className="space-y-6">
      {/* Welcome Section */}
      <div className="bg-gradient-to-r from-government-600 to-government-700 rounded-xl p-6 text-white">
        <h1 className="text-2xl font-bold">
          Hoş Geldiniz, {user?.firstName || 'Kullanıcı'}!
        </h1>
        <p className="mt-2 text-government-100">
          Kurumsal Başvuru ve Ödeme Yönetim Platformu
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map((stat) => (
          <div key={stat.name} className="card">
            <div className="flex items-center">
              <div className={`${stat.color} p-3 rounded-lg text-2xl`}>
                {stat.icon}
              </div>
              <div className="ml-4">
                <p className="text-sm text-gray-600">{stat.name}</p>
                <p className="text-2xl font-bold text-gray-900">{stat.value}</p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Quick Actions */}
      <div className="card">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Hızlı İşlemler</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {quickActions.map((action) => (
            <Link
              key={action.name}
              to={action.href}
              className="flex flex-col items-center p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
            >
              <span className="text-3xl mb-2">{action.icon}</span>
              <span className="font-medium text-gray-900">{action.name}</span>
              <span className="text-xs text-gray-500 text-center mt-1">{action.description}</span>
            </Link>
          ))}
        </div>
      </div>

      {/* Recent Activity */}
      <div className="card">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Son İşlemler</h2>
        <div className="space-y-3">
          {[
            { action: 'Başvuru oluşturuldu', time: '2 saat önce', status: 'success' },
            { action: 'Ödeme tamamlandı', time: '1 gün önce', status: 'success' },
            { action: 'Belge yüklendi', time: '3 gün önce', status: 'info' },
            { action: 'Başvuru onaylandı', time: '1 hafta önce', status: 'success' },
          ].map((item, index) => (
            <div key={index} className="flex items-center justify-between py-2 border-b border-gray-100 last:border-0">
              <div className="flex items-center">
                <span className={`w-2 h-2 rounded-full mr-3 ${
                  item.status === 'success' ? 'bg-green-500' : 'bg-blue-500'
                }`}></span>
                <span className="text-gray-900">{item.action}</span>
              </div>
              <span className="text-sm text-gray-500">{item.time}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Security Notice */}
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <div className="flex">
          <div className="flex-shrink-0">
            <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-yellow-800">Güvenlik Bildirimi</h3>
            <p className="mt-1 text-sm text-yellow-700">
              Tüm işlemleriniz güvenlik amacıyla kayıt altına alınmaktadır. 
              Şüpheli bir aktivite fark ederseniz derhal sistem yöneticisine bildirin.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
