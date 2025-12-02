import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BackupScheduleListComponent } from './backup-schedule-list.component';

describe('BackupScheduleListComponent', () => {
  let component: BackupScheduleListComponent;
  let fixture: ComponentFixture<BackupScheduleListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BackupScheduleListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BackupScheduleListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
