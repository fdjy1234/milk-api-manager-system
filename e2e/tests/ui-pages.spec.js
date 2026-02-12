const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

const screenshotDir = path.join(__dirname, '..', 'screenshots');

const pages = [
  {
    name: 'api-list',
    path: '/apis',
    ready: async (page) => page.getByText(/API Governance & Inventory/).first().waitFor()
  },
  {
    name: 'api-inventory',
    path: '/api-inventory',
    ready: async (page) => page.locator('.alert.alert-info').first().waitFor()
  },
  {
    name: 'consumers',
    path: '/consumers',
    ready: async (page) => page.getByText(/Consumers/).first().waitFor()
  },
  {
    name: 'blacklist',
    path: '/blacklist',
    ready: async (page) => page.getByText(/IP Blacklist Management/).first().waitFor()
  },
  {
    name: 'consumer-analytics',
    path: '/consumer-analytics',
    ready: async (page) => page.getByText(/Consumer Statistics/).first().waitFor()
  },
  {
    name: 'reports',
    path: '/reports',
    ready: async (page) => page.getByText(/Consumer Stats/).first().waitFor()
  },
  {
    name: 'sync-status',
    path: '/sync-status',
    ready: async (page) => page.getByText(/Group Sync Status/).first().waitFor()
  }
];

test.describe('Milk Admin UI pages', () => {
  test.beforeAll(() => {
    fs.mkdirSync(screenshotDir, { recursive: true });
  });

  for (const pageDef of pages) {
    test(`${pageDef.name} page renders and captures screenshot`, async ({ page }) => {
      await page.goto(pageDef.path, { waitUntil: 'domcontentloaded' });
      await pageDef.ready(page);

      const screenshotPath = path.join(screenshotDir, `${pageDef.name}.png`);
      await page.screenshot({ path: screenshotPath, fullPage: true });

      await expect(page).toHaveURL(new RegExp(`${pageDef.path.replace('/', '\\/')}$`));
    });
  }
});
