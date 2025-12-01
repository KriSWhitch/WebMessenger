import type { NextConfig } from 'next';

import dotenv from 'dotenv';
import { resolve } from 'path';

const env = dotenv.config({
  path: resolve(process.cwd(), `.env.${process.env.NODE_ENV || 'development'}`),
}).parsed;

const nextConfig: NextConfig = {
  env,
  images: {
    remotePatterns: [
      { protocol: 'https', hostname: 'www.dropbox.com' },
      { protocol: 'https', hostname: 'dl.dropboxusercontent.com' },
      { protocol: 'http', hostname: 'localhost' },
    ],
  },
  reactStrictMode: true,
};

export default nextConfig;
