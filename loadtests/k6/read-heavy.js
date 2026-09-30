import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:5157";
const adminEmail = __ENV.ADMIN_EMAIL || "admin@beerapi.com";
const adminPassword = __ENV.ADMIN_PASSWORD || "Admin@123!";

export const options = {
  scenarios: {
    read_heavy: {
      executor: "constant-vus",
      vus: 20,
      duration: "60s",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<500"],
  },
};

export function setup() {
  let loginResponse;
  for (let attempt = 0; attempt < 30; attempt++) {
    loginResponse = http.post(
      `${baseUrl}/api/auth/login`,
      JSON.stringify({ email: adminEmail, password: adminPassword }),
      { headers: { "Content-Type": "application/json" } },
    );
    if (loginResponse.status === 200) break;
    sleep(2);
  }
  if (!loginResponse || loginResponse.status !== 200) {
    throw new Error(`Admin login failed with status ${loginResponse.status}`);
  }

  const token = loginResponse.json("accessToken");
  const params = {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  };
  const breweriesResponse = http.get(`${baseUrl}/api/breweries?page=1&pageSize=100`, params);
  const wholesalersResponse = http.get(`${baseUrl}/api/wholesalers?page=1&pageSize=100`, params);
  if (breweriesResponse.status !== 200 || wholesalersResponse.status !== 200) {
    throw new Error("Could not load the catalog for the benchmark setup");
  }

  const breweries = breweriesResponse.json("items");
  const wholesalers = wholesalersResponse.json("items");
  const wholesaler = wholesalers[0];
  let brewery;
  let beer;

  for (const candidate of breweries) {
    const beersResponse = http.get(`${baseUrl}/api/breweries/${candidate.id}/beers`, params);
    const beers = beersResponse.json();
    if (beersResponse.status === 200 && beers.length > 0) {
      brewery = candidate;
      beer = beers[0];
      break;
    }
  }

  if (!brewery || !beer || !wholesaler) {
    throw new Error("The database needs at least one beer and one wholesaler");
  }

  const saleResponse = http.post(
    `${baseUrl}/api/sales`,
    JSON.stringify({ beerId: beer.id, wholesalerId: wholesaler.id, quantity: 100 }),
    params,
  );
  if (saleResponse.status !== 201) {
    throw new Error(`Could not seed wholesaler stock: ${saleResponse.status}`);
  }

  return { token, breweryId: brewery.id, wholesalerId: wholesaler.id, beerId: beer.id };
}

export default function (data) {
  const params = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json",
    },
  };
  const responses = http.batch([
    { method: "GET", url: `${baseUrl}/api/breweries?page=1&pageSize=20`, params },
    { method: "GET", url: `${baseUrl}/api/wholesalers?page=1&pageSize=20`, params },
    { method: "GET", url: `${baseUrl}/api/breweries/${data.breweryId}/beers`, params },
    { method: "GET", url: `${baseUrl}/api/wholesalers/${data.wholesalerId}/beers`, params },
    {
      method: "POST",
      url: `${baseUrl}/api/wholesalers/${data.wholesalerId}/quote`,
      body: JSON.stringify({ items: [{ beerId: data.beerId, quantity: 1 }] }),
      params,
    },
  ]);

  for (const response of responses) {
    check(response, { "status is below 400": (item) => item.status < 400 });
  }
  sleep(1);
}