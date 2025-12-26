/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: 'http://localhost:5000/api/:path*',
      },
    ];
  },
  async headers() {
    return [
      {
        source: '/api/:path*',
        headers: [
          {
            key: 'x-tenant-id',
            value: '00000000-0000-0000-0000-000000000001', // Default tenant for development
          },
        ],
      },
    ];
  },
};

module.exports = nextConfig;

