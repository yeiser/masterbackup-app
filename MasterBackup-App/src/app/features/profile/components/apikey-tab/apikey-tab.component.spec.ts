import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApikeyTabComponent } from './apikey-tab.component';

describe('ApikeyTabComponent', () => {
  let component: ApikeyTabComponent;
  let fixture: ComponentFixture<ApikeyTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApikeyTabComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ApikeyTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
