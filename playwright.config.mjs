import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './tests/browser', timeout: 180000, expect: { timeout: 30000 }, workers: 1, retries: 0,
  outputDir: 'artifacts/test-results',
  reporter: [['list'], ['html', { outputFolder: 'artifacts/playwright-report', open: 'never' }], ['junit', { outputFile: 'artifacts/browser-results.xml' }]],
  use: { viewport: { width: 1440, height: 980 }, deviceScaleFactor: 1, headless: true, actionTimeout: 30000, trace: 'retain-on-failure', screenshot: 'only-on-failure', launchOptions: { args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] } }
});
