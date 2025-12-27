/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Removed rewrites - using API routes instead to properly forward headers
  images: {
    unoptimized: true, // Allow unoptimized images for logo
  },
};

module.exports = nextConfig;

