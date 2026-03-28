import { getTestBed } from '@angular/core/testing';
import {
  BrowserTestingModule,
  platformBrowserTesting,
} from '@angular/platform-browser/testing';

export function ensureTestBedInit(): void {
  const testBed = getTestBed();
  try {
    testBed.initTestEnvironment(
      BrowserTestingModule,
      platformBrowserTesting(),
    );
  } catch {
    // Already initialized — ignore
  }
}

ensureTestBedInit();
