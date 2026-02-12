# E2E UI Tests (Playwright)

This folder contains Playwright tests that visit each admin UI page and save screenshots.

## Prerequisites
- Admin UI is running (default: http://localhost:55894)
- Node.js and npm installed

## Install

```bash
cd e2e
npm install
npx playwright install
```

## Run tests

```bash
cd e2e
npm test
```

## Outputs
- Screenshots: `e2e/screenshots/`
- HTML report: `e2e/test-report/`

## Notes
- Change the UI base URL by setting `BASE_URL`.

```bash
$env:BASE_URL = "http://localhost:5000"
cd e2e
npm test
```
