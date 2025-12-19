import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InformationTabComponent } from './information-tab.component';

describe('InformationTabComponent', () => {
  let component: InformationTabComponent;
  let fixture: ComponentFixture<InformationTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InformationTabComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InformationTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
