const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  page.on('console', msg => console.log('BROWSER_CONSOLE:', msg.type(), msg.text()));
  page.on('pageerror', err => console.log('PAGEERROR:', err.message));

  await page.addInitScript(() => {
    const base64 = (value) => btoa(unescape(encodeURIComponent(JSON.stringify(value))))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
    const payload = {
      unique_name: 'SV2024001',
      nameid: '1',
      StudentId: '1',
      role: 'User',
      exp: 4102444800
    };
    const token = `${base64(payload)}.signature`;
    localStorage.setItem('accessToken', token);
    localStorage.setItem('refreshToken', 'fake-refresh');
  });

  await page.goto('http://localhost:5174/', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
  console.log('LOADED_URL=', page.url());

  await page.getByText('Hồ sơ của tôi', { exact: true }).click();
  await page.waitForTimeout(700);
  console.log('AFTER_CLICK_PROFILE=', page.url());

  await page.getByText('Doanh nghiệp', { exact: true }).click();
  await page.waitForTimeout(700);
  console.log('AFTER_CLICK_COMPANIES=', page.url());

  await page.getByText('Hồ sơ của tôi', { exact: true }).click();
  await page.waitForTimeout(700);
  console.log('AFTER_CLICK_PROFILE_2=', page.url());

  await browser.close();
})();
