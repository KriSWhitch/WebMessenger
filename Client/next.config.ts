import type { NextConfig } from "next";

import dotenv from 'dotenv';
import { resolve } from 'path';

const env = dotenv.config({ path: resolve(process.cwd(), `.env.${process.env.NODE_ENV || 'development'}`) }).parsed;

const nextConfig: NextConfig = {
  env,
  images: {
    domains: ['raw.githubusercontent.com']
  }
};

export default nextConfig;
